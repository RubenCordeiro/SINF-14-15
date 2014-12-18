using System.Collections.Generic;

namespace Picking.Lib_Primavera.Model
{
    public class PickingSelection
    {
        public ICollection<int> Orders { get; set; }

        public string Facility { get; set; }

        public double AvailableCapacity { get; set; }
    }
}
