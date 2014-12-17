using System;
using System.Collections.Generic;

namespace Picking.Lib_Primavera.Model
{
    public class PickingList
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string PickerName { get; set; }

        public IEnumerable<PickingItem> Items { get; set; }

        public IEnumerable<OrderLine> SkippedOrders { get; set; } 
    }
}
