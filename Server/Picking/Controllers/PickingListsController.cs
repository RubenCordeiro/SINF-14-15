using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web.Http;
using Interop.GcpBE800;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class PickingListsController : ApiController
    {
        public PickingListsController()
        {
            _company = new Company("BELAFLOR", "", "");
        }

        public string Get()
        {
            
            try
            {
                var data = DateTime.Now;

                var doc = new GcpBEDocumentoStock();

                doc.set_Tipodoc("TRA");

                _company.Engine.Comercial.Stocks.PreencheDadosRelacionados(doc);
                
                doc.set_ArmazemOrigem("A1"); // TODO: ArmazemOrigem comes from Order ? or from parameters
                doc.set_DataDoc(DateTime.Now);

                var lines = new GcpBELinhasDocumentoStock();

                var itemLines = _company.Engine.Comercial.Stocks.SugereArtigoLinhas(Artigo: "FC.0002", Armazem: "A1", Quantidade: 10D, Localizacao: "A1.S2.P1");

                // _company.Engine.Comercial.Stocks.AdicionaLinha(ClsDocStk: doc, Artigo: "FC.0002", Armazem: "A1", Quantidade: 10D, Localizacao: "A1.S2.P1");


                for (var i = 1; i <= itemLines.NumItens; ++i)
                {
                    var line = itemLines.get_Edita(i);
                    line.set_LocalizacaoOrigem("A1.S1.P3");
                    line.set_DataStock(data);

                    lines.Insere(line);
                }

                doc.set_Linhas(lines);

                var avisos = "";

                _company.Engine.Comercial.Stocks.Actualiza(doc, ref avisos);

                return avisos;

            }
            catch (COMException ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, "COMException: " + ex));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, "Exception: " + ex));
            }
        }

        public PickingWave Post(PickingSelection selection)
        {
            var pickingItems = new List<PickingItem>();
            var skippedOrders = new List<OrderLine>();

            foreach (var orderId in selection.Orders)
            {
                Order order;
                try
                {
                    order = _company.GetOrder(orderId);
                    if (order == null)
                        continue;
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var orderLine in order.OrderLines)
                {
                    if (orderLine.Picked)
                        continue;

                    var stock = GetStock(orderLine.ItemId)
                        .Where(itemStock => itemStock.Stock > 0 && itemStock.StorageFacility == selection.Facility)
                        .OrderByDescending(itemStock => itemStock.Stock) // Prioritize by stock quantity
                        .Where(itemStock => Location.FromString(itemStock.StorageLocation) != null) // Only valid locations
                        .ToList();

                    if (stock.Sum(itemStock => itemStock.Stock) < orderLine.Quantity)
                    {
                        skippedOrders.Add(orderLine);
                        continue;
                    }

                    ItemStock previousStockLocation = null;
                    while (orderLine.Quantity > 0)
                    {
                        var stockLocation = previousStockLocation == null ? stock[0] : GetClosestStockLocation(stock, previousStockLocation);
                        previousStockLocation = stockLocation;
                        if (stockLocation == null)
                            continue;

                        double quantity;
                        if (stockLocation.Stock > orderLine.Quantity)
                        {
                            quantity = orderLine.Quantity;
                            orderLine.Quantity = 0;
                        }
                        else
                        {
                            quantity = stockLocation.Stock;
                            orderLine.Quantity -= stockLocation.Stock;
                        }

                        stockLocation.Stock -= quantity;

                        if (Math.Abs(quantity) < Double.Epsilon)
                            break;

                        var pickingItem = new PickingItem
                        {
                            ItemId = orderLine.ItemId,
                            ItemDescription = orderLine.ItemDescription,
                            Quantity = quantity,
                            Unit = orderLine.Unit,
                            StorageFacility = stockLocation.StorageFacility,
                            StorageLocation = stockLocation.StorageLocation,
                        };

                        pickingItems.Add(pickingItem);
                        _company.MarkOrderLinePicked(order, orderLine);
                    }
                }
            }

            if (pickingItems.Count > 0)
            {
                _company.InsertPickingItems(pickingItems);
                // TODO: if pickingItems is not empty:
                // Mark orderlines as picked - DONE
                // Save pickingItems in primavera's database - DONE
                // Create transfer documents
                // Update stocks
            }

            return new PickingWave {Items = pickingItems, SkippedOrders = skippedOrders};
        }

        private ItemStock GetClosestStockLocation(IEnumerable<ItemStock> stock, ItemStock previousStockLocation)
        {
            var d = double.PositiveInfinity;
            ItemStock result = null;

            var loc1 = Location.FromString(previousStockLocation.StorageLocation);

            foreach (var s in stock)
            {
                var loc2 = Location.FromString(s.StorageLocation);
                var dist = Location.GetDistance(loc1, loc2);

                if (dist < d)
                {
                    result = s;
                    d = dist;
                }
            }

            return result;
        }

        private class Location
        {
            public static Location FromString(string location)
            {
                Contract.Requires(location != null);

                var match = Regex.Match(location, "A([0-9]+)\\.C([0-9]+)\\.S([0-9]+)"); // A1.C1.S1
                if (!match.Success)
                    return null;

                int a = int.Parse(match.Groups[1].ToString());
                int c = int.Parse(match.Groups[2].ToString());
                int s = int.Parse(match.Groups[3].ToString());

                return new Location(a, c, s);
            }

            public Location(int a, int c, int s)
            {
                Facility = a;
                Corridor = c;
                Section = s;
            }

            public static double GetDistance(Location loc1, Location loc2)
            {
                Contract.Requires(loc1 != null);
                Contract.Requires(loc2 != null);
                Contract.Requires(loc1.Facility == loc2.Facility);

                var v = Math.Abs(loc1.Corridor - loc2.Corridor);
                var h = Math.Abs(loc1.Section - loc2.Section);

                return v + h;
            }

            public int Facility { get; set; } // A
            public int Corridor { get; set; } // C
            public int Section { get; set; }  // S
        }

        private IEnumerable<ItemStock> GetStock(string itemId)
        {
            return _company.ListItemStock().Where(stock => stock.Item == itemId);
        }

        private readonly Company _company;
    }
}
