using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace TPMechanical.HangerPlacement.Rules
{
    public static class HangerRuleOptions
    {
        public static List<string> Types { get; } =
            new List<string>
            {
                "Pipework",
                "Duct - Rectangular",
                "Duct - Round",
                "Duct - Oval"
            };


        public static List<string> Materials { get; } =
            new List<string>
            {
                "ABS",
                "Aluminum",
                "Carbon Steel",
                "Cast Iron",
                "Copper",
                "CPVC",
                "Ductile Iron",
                "Galvanized",
                "Galvanized Spiral Coil",
                "HDPE",
                "Koolduct",
                "Mild Steel",
                "Paint Grip",
                "Paint Grip Spiral Coil",
                "Perforated",
                "Polyethylene",
                "PVC",
                "PVDF",
                "Stainless Steel",
                "Stainless - 304"
            };


        public static List<string> SizeByOptions { get; } =
            new List<string>
            {
                "Width",
                "Height",
                "Diameter",
                "Half Perimeter"
            };


        public static List<string> HangerTypes { get; } =
            new List<string>
            {
                "Clevis Hanger",
                "Half Strap Hanger",
                "Round Strap Hanger",
                "Trapeze Hanger"
            };
    }
}
