using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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

        public IEnumerable<PickingItem> Post(ICollection<int> orders)
        {
            if (orders.Count == 0)
                return new List<PickingItem>();

            var allStocks = _company.ListItemStock();

            var pickingItems = new List<PickingItem>();
            foreach (var orderId in orders)
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

                var removeOrderLines = new List<OrderLine>();
                foreach (var orderLine in order.OrderLines)
                {
                    double totalStock = 0;
                    foreach (var itemStock in allStocks)
                    {
                        if (orderLine.ItemId == itemStock.Item)
                        {
                            totalStock += itemStock.Stock;
                            itemStock.Stock -= orderLine.Quantity;
                        }
                    }

                    if (totalStock < orderLine.Quantity)
                    {
                        // Stock not enough to fullfil order
                        removeOrderLines.Add(orderLine);
                    }
                }

                foreach (var ol in removeOrderLines)
                {
                    order.OrderLines.Remove(ol);
                }

                foreach (var orderLine in order.OrderLines)
                {
                    foreach (var itemStock in allStocks)
                    {
                        if (orderLine.ItemId == itemStock.Item)
                        {
                            var orderLineQuantity = orderLine.Quantity;

                            while (orderLineQuantity > 0)
                            {
                                double quantity;
                                if (itemStock.Stock > orderLineQuantity)
                                {
                                    quantity = orderLineQuantity;
                                    orderLineQuantity = 0;
                                }
                                else
                                {
                                    quantity = itemStock.Stock;
                                    orderLineQuantity -= itemStock.Stock;
                                }

                                var pickingItem = new PickingItem
                                {
                                    ItemId = orderLine.ItemId,
                                    ItemDescription = orderLine.ItemDescription,
                                    Quantity = quantity,
                                    Unit = orderLine.Unit,
                                    StorageFacility = itemStock.StorageFacility,
                                    StorageLocation = itemStock.StorageLocation,
                                };

                                pickingItems.Add(pickingItem);
                            }
                        }
                    }
                }

                // TODO: Add XPTO algorithm to select best pickingItems
            }

            return pickingItems;
        }

        private readonly Company _company;
    }
}
