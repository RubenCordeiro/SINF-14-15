namespace Picking.Lib_Primavera.Model
{
    public class PickingItem
    {
        public string OrderLineId { get; set; }

        public string ItemId { get; set; }

        public string ItemDescription { get; set; }

        public string StorageFacility { get; set; }

        public string StorageLocation { get; set; }

        public double Quantity { get; set; }

        public double PickedQuantity { get; set; }

        public string Unit { get; set; }
    }
}
