using System;
using System.Collections.Generic;

namespace Picking.Lib_Primavera.Model
{
    public class PutawayList
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string PickerName { get; set; }

        public IEnumerable<PutawayItem> Items { get; set; }

        public IEnumerable<SupplyLine> SkippedSupplies { get; set; } 
    }
}
