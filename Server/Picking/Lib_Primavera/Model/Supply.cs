using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Picking.Lib_Primavera.Model
{
    public class Supply
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public string EntityName { get; set; }

        public int NumDoc { get; set; }

        public DateTime Data { get; set; }

        public double TotalMerc { get; set; }

        public string Serie { get; set; }

        public List<SupplyLine> SupplyLines { get; set; }
    }

    public class SupplyLine
    {
        public string ItemId { get; set; }

        public string ItemDescription { get; set; }

        public int LineNo { get; set; }

        public string IdCabecCompras { get; set; }

        public double Quantity { get; set; }

        public string Unit { get; set; }

        public double Discount { get; set; }

        public double UnitPrice { get; set; }

        public double TotalINet { get; set; }

        public double TotalNet { get; set; }

        public bool Putaway { get; set; }

        public double PutawayQuantity { get; set; }

        public string Id { get; set; }
    }
}
