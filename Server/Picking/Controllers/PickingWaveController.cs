using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Picking.Lib_Primavera;
using Picking.Lib_Primavera.Model;

namespace Picking.Controllers
{
    public class PickingWaveController : ApiController
    {
        // POST /api/pickingwave/
        public IEnumerable<string> Post(PickingList pickingList)
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

        private readonly Company _company = new Company("BELAFLOR");
    }
}
