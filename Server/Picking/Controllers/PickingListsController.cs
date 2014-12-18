using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                        var stockLocation = previousStockLocation == null ? stock[0] : Company.GetClosestLocation(stock, previousStockLocation);
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

                _company.SetOrderLinePickedQuantity(item.OrderLineId, item.PickedQuantity);
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

        private IEnumerable<ItemStock> GetStock(string itemId)
        {
            return _company.ListItemStock().Where(stock => stock.Item == itemId);
        }

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}
