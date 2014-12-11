﻿namespace Picking.Lib_Primavera.Model
{
    public class PickingItem
    {
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public string StorageFacility { get; set; }
        public string StorageLocation { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }
}