using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class PickingListsController : ApiController
    {
        // POST /api/pickinglists/
        public PickingList Post(PickingSelection selection)
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

                    if (Math.Abs(orderLine.PickedQuantity - orderLine.Quantity) < Double.Epsilon * 100)
                        continue;

                    var stock = GetStock(orderLine.Item.Id)
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
                            OrderLineId = orderLine.Id,
                            Item = orderLine.Item,
                            Quantity = quantity,
                            Unit = orderLine.Unit,
                            StorageFacility = stockLocation.StorageFacility,
                            StorageLocation = stockLocation.StorageLocation,
                        };

                        pickingItems.Add(pickingItem);
                        _company.MarkOrderLinePicked(orderLine.Id);
                    }
                }
            }

            return new PickingList {Items = pickingItems, SkippedOrders = skippedOrders};
        }

        // PATCH /api/pickinglists
        public IEnumerable<string> Patch(PickingList pickingList)
        {
            var errors = new List<string>();
            var itemsList = pickingList.Items.ToList();
            if (itemsList.Count == 0)
            {
                errors.Add("Empty input.");
                return errors;
            }

            foreach (var item in itemsList)
            {
                if (item.PickedQuantity < item.Quantity)
                {
                    var quantity = item.Quantity - item.PickedQuantity;
                    var err1 = _company.GenerateStockRemovalDocument(item, quantity);
                    if (!string.IsNullOrWhiteSpace(err1))
                        errors.Add(err1);

                    _company.MarkOrderLinePicked(item.OrderLineId, false);
                }
            }

            _company.InsertPickingItems(itemsList);

            var err2 = _company.GenerateStockTransferDocument(itemsList);
            if (!string.IsNullOrWhiteSpace(err2))
                errors.Add(err2);

            return errors;
        }

        // GET /api/pickinglists
        public IEnumerable<PickingList> Get()
        {
            return _company.ListPickingLists();
        }

        // GET /api/pickinglists/<id>
        public PickingList Get(int id)
        {
            var pickingList = _company.GetPickingList(id);
            if (pickingList == null)
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return pickingList;
        }

        private static ItemStock GetClosestStockLocation(IEnumerable<ItemStock> stock, ItemStock previousStockLocation)
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

        private readonly Company _company = new Company(Company.COMPANY);
    }
}
