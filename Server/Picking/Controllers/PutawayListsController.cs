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
    public class PutawayListsController : ApiController
    {
        // POST /api/putawaylists
        public PutawayList Post(PutawaySelection selection)
        {
            var putawayItems = new List<PutawayItem>();
            var skippedSupplies = new List<SupplyLine>();

            foreach (var supplyId in selection.Supplies)
            {
                Supply supply;
                try
                {
                    supply = _company.GetSupply(supplyId);
                    if (supply == null)
                        continue;
                }
                catch (Exception)
                {
                    continue;
                }

                string previousStockLocation = null;
                foreach (var supplyLine in supply.SupplyLines)
                {
                    if (supplyLine.Putaway)
                        continue;

                    //if (Math.Abs(supplyLine.PutawayQuantity - supplyLine.Quantity) < Double.Epsilon * 100)
                    //    continue;

                    var stock = GetStock(supplyLine.Item.Id)
                        .Where(itemStock => itemStock.Stock > 0 && itemStock.StorageFacility == selection.Facility)
                        .OrderByDescending(itemStock => itemStock.Stock)
                        .ToList();

                    string location;
                    if (stock.Count > 0) // if there are locations with the same items
                        location = previousStockLocation == null ?
                            stock[0].StorageLocation :
                            Company.GetClosestLocation(stock.Select(itemStock => itemStock.StorageLocation), previousStockLocation);
                    else
                    {
                        var locations = _company.ListStorageLocations()
                            .Where(storageLocation => Location.FromString(storageLocation.Location) != null)
                            .Select(storageLocation => storageLocation.Location)
                            .ToList();

                        if (locations.Count == 0)
                        {
                            skippedSupplies.Add(supplyLine);
                            continue;
                        }

                        location = previousStockLocation == null ? locations[0] :
                            Company.GetClosestLocation(locations, previousStockLocation);
                    }

                    previousStockLocation = location;

                    var putawayItem = new PutawayItem
                    {
                        SupplyLineId = supplyLine.Id,
                        Item = supplyLine.Item,
                        Quantity = supplyLine.Quantity,
                        Unit = supplyLine.Unit,
                        StorageFacility = Company.ExtractFacility(location),
                        StorageLocation = location,
                    };

                    putawayItems.Add(putawayItem);
                    _company.MarkSupplyLinePutaway(supplyLine.Id);
                }
            }

            return new PutawayList {Items = putawayItems, SkippedSupplies = skippedSupplies};
        }

        // PATCH /api/putawaylists
        public IEnumerable<string> Patch(PutawayList putawayList)
        {
            var errors = new List<string>();
            var itemsList = putawayList.Items.ToList();
            if (itemsList.Count == 0)
            {
                errors.Add("Empty input.");
                return errors;
            }

            foreach (var item in itemsList.Where(item => item.StorageLocation == "none"))
            {
                _company.MarkSupplyLinePutaway(item.SupplyLineId, false);
            }

            //foreach (var item in itemsList)
            //{
            //    //if (item.PickedQuantity < item.Quantity)
            //    //{
            //    //    var quantity = item.Quantity - item.PickedQuantity;
            //    //    var err1 = _company.GenerateStockRemovalDocument(item, quantity);
            //    //    if (!string.IsNullOrWhiteSpace(err1))
            //    //        errors.Add(err1);
            //    //
            //    //    _company.MarkOrderLinePicked(item.OrderLineId, false);
            //    //}
            //
            //    //_company.SetOrderLinePickedQuantity(item.OrderLineId, item.PickedQuantity);
            //}

            _company.InsertPutawayItems(itemsList);

            var err2 = _company.GenerateStockTransferDocument(itemsList);
            if (!string.IsNullOrWhiteSpace(err2))
                errors.Add(err2);

            return errors;
        }

        // GET /api/putawaylists
        public IEnumerable<PutawayList> Get()
        {
            return _company.ListPutawayLists();
        }

        // GET /api/putawaylists/<id>
        public PutawayList Get(int id)
        {
            var putawayList = _company.GetPutawayList(id);
            if (putawayList == null)
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));

            return putawayList;
        }

        private IEnumerable<ItemStock> GetStock(string itemId)
        {
            return _company.ListItemStock().Where(stock => stock.Item == itemId && Location.FromString(stock.StorageLocation) != null);
        }

        private readonly Company _company = new Company(Company.TargetCompany);
    }
}
