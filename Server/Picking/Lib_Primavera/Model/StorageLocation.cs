using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Picking.Lib_Primavera.Model
{
    public class StorageLocation
    {
        public string Id { get; set; }
        public string Location { get; set; }
        public string StorageFacility { get; set; }
        public string Description { get; set; }
        public string IdParent { get; set; }
    }
}