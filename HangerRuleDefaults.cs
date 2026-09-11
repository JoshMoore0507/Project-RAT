using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace TPMechanical.QAQC
{
    public static class HangerRuleDefaults
    {
        public static List<HangerRule> GetDefaults()
        {
            return new List<HangerRule>
            {
                new HangerRule
                {
                    Use = true,
                    Type = "Pipework",
                    Service = "ANY",
                    Material = "Carbon Steel",
                    SizeBy = "Width",
                    SizeLessThanOrEqual = "999'-0\"",
                    Spacing = "12'-0\"",
                    FromEnd = "1'-0\"",
                    FromJoint = "NA",
                    FromBranchTap = "1'-0\"",
                    HangerType = "Clevis Hanger",
                    HangerWithInsulation = ""
                },

                new HangerRule
                {
                    Use = true,
                    Type = "Pipework",
                    Service = "ANY",
                    Material = "Cast Iron",
                    SizeBy = "Width",
                    SizeLessThanOrEqual = "999'-0\"",
                    Spacing = "8'-0\"",
                    FromEnd = "1'-0\"",
                    FromJoint = "1'-0\"",
                    FromBranchTap = "1'-0\"",
                    HangerType = "Clevis Hanger",
                    HangerWithInsulation = ""
                },

                new HangerRule
                {
                    Use = true,
                    Type = "Duct - Rectangular",
                    Service = "ANY",
                    Material = "Galvanized",
                    SizeBy = "Half Perimeter",
                    SizeLessThanOrEqual = "999'-0\"",
                    Spacing = "8'-0\"",
                    FromEnd = "1'-0\"",
                    FromJoint = "NA",
                    FromBranchTap = "1'-0\"",
                    HangerType = "Half Strap Hanger",
                    HangerWithInsulation = ""
                },

                new HangerRule
                {
                    Use = true,
                    Type = "Duct - Round",
                    Service = "ANY",
                    Material = "Galvanized",
                    SizeBy = "Width",
                    SizeLessThanOrEqual = "999'-0\"",
                    Spacing = "8'-0\"",
                    FromEnd = "1'-0\"",
                    FromJoint = "NA",
                    FromBranchTap = "1'-0\"",
                    HangerType = "Round Strap Hanger",
                    HangerWithInsulation = ""
                }
            };
        }
    }
}