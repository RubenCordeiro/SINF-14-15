namespace Picking.Lib_Primavera.Model
{
    public class OrderLine
    {
        public string ItemId { get; set; }

        public string ItemDescription { get; set; }

        public int LineNo { get; set; }

        public string IdCabecDoc { get; set; }

        public double Quantity { get; set; }

        public string Unit { get; set; }

        public double Discount { get; set; }

        public double UnitPrice { get; set; }

        public double TotalINet { get; set; }

        public double TotalNet { get; set; }

        public bool Picked { get; set; }

        public string Id { get; set; }
    }
}
