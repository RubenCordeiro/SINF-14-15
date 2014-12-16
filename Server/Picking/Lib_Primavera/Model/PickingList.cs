using System.Collections.Generic;

namespace Picking.Lib_Primavera.Model
{
    public class PickingList
    {
        public IEnumerable<PickingItem> Items { get; set; }

        public IEnumerable<OrderLine> SkippedOrders { get; set; } 
    }
}
