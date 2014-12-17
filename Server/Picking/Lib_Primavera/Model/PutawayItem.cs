namespace Picking.Lib_Primavera.Model
{
    public class PutawayItem
    {
        public string SupplyLineId { get; set; }

        public Item Item { get; set; }

        public string StorageFacility { get; set; }

        public string StorageLocation { get; set; }

        public double Quantity { get; set; }

        public double PutawayQuantity { get; set; }

        public string Unit { get; set; }
    }
}
