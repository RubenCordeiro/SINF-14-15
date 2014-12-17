using System;
using System.Collections.Generic;

namespace Picking.Lib_Primavera.Model
{
    public class Order
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public string EntityName { get; set; }

        public int NumDoc { get; set; }

        public DateTime Data { get; set; }

        public double TotalMerc { get; set; }

        public string Serie { get; set; }

        public List<OrderLine> OrderLines { get; set; }
    }

    public class OrderLine
    {
        public Item Item { get; set; }

        public int LineNo { get; set; }

        public string IdCabecDoc { get; set; }

        public double Quantity { get; set; }

        public string Unit { get; set; }

        public double Discount { get; set; }

        public double UnitPrice { get; set; }

        public double TotalINet { get; set; }

        public double TotalNet { get; set; }

        public bool Picked { get; set; }

        public double PickedQuantity { get; set; }

        public string Id { get; set; }
    }
}
