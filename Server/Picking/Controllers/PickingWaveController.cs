using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class PickingWaveController : ApiController
    {
        public IEnumerable<string> Post(PickingWave pickingWave)
        {
            var errors = new List<string>();
            var itemsList = pickingWave.Items.ToList();
            if (itemsList.Count == 0)
            {
                errors.Add("Empty input.");
                return errors;
            }

            var facility = itemsList[0].StorageFacility;

            foreach (var item in itemsList)
            {
                if (item.PickedQuantity < item.Quantity)
                {
                    var quantity = item.Quantity - item.PickedQuantity;
                    var err1 = _company.GenerateStockRemovalDocument(item, quantity);
                    if (!string.IsNullOrWhiteSpace(err1))
                        errors.Add(err1);
                }
            }

            _company.InsertPickingItems(itemsList);

            var err2 = _company.GenerateStockTransferDocument(itemsList, facility);
            if (!string.IsNullOrWhiteSpace(err2))
                errors.Add(err2);

            return errors;
        }

        private readonly Company _company = new Company("BELAFLOR", "", "");
    }
}
