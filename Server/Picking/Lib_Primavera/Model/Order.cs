using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Picking.Lib_Primavera.Model
{
    public class Order
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public int NumDoc { get; set; }

        public DateTime Data { get; set; }

        public double TotalMerc { get; set; }

        public string Serie { get; set; }

        public List<OrderLine> OrderLines { get; set; }
    }
}