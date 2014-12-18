using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Picking.Lib_Primavera.Model
{
    public class LocationHelper
    {
        public static LocationHelper FromString(string location)
        {
            Contract.Requires(location != null);

            var match = Regex.Match(location, "A([0-9]+)\\.C([0-9]+)\\.S([0-9]+)"); // A1.C1.S1
            if (!match.Success)
                return null;

            int a = Int32.Parse(match.Groups[1].ToString(), CultureInfo.InvariantCulture);
            int c = Int32.Parse(match.Groups[2].ToString(), CultureInfo.InvariantCulture);
            int s = Int32.Parse(match.Groups[3].ToString(), CultureInfo.InvariantCulture);

            return new LocationHelper(a, c, s);
        }

        public LocationHelper(int a, int c, int s)
        {
            Facility = a;
            Corridor = c;
            Section = s;
        }

        public override string ToString()
        {
            return string.Format("A{0}.C{1}.S{2}", Facility, Corridor, Section);
        }

        public static double GetDistance(LocationHelper loc1, LocationHelper loc2)
        {
            Contract.Requires(loc1 != null);
            Contract.Requires(loc2 != null);
            Contract.Requires(loc1.Facility == loc2.Facility);

            var v = Math.Abs(loc1.Corridor - loc2.Corridor);
            var h = Math.Abs(loc1.Section - loc2.Section);

            return v + h;
        }

        public int Facility { get; set; } // A
        public int Corridor { get; set; } // C
        public int Section { get; set; }  // S
    }
}
