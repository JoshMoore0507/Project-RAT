using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPMechanical.QAQC
{
    public class HangerRule
    {
        public bool Use { get; set; } = true;

        public string Type { get; set; } = "";
        public string Service { get; set; } = "ANY";
        public string Material { get; set; } = "";

        public string SizeBy { get; set; } = "";
        public string SizeLessThanOrEqual { get; set; } = "";

        public string Spacing { get; set; } = "";
        public string FromEnd { get; set; } = "";
        public string FromJoint { get; set; } = "";
        public string FromBranchTap { get; set; } = "";

        public string HangerType { get; set; } = "";
        public string HangerWithInsulation { get; set; } = "";

        public string PendingAction { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public HangerRule Clone()
        {
            return new HangerRule
            {
                Use = Use,
                Type = Type,
                Service = Service,
                Material = Material,
                SizeBy = SizeBy,
                SizeLessThanOrEqual = SizeLessThanOrEqual,
                Spacing = Spacing,
                FromEnd = FromEnd,
                FromJoint = FromJoint,
                FromBranchTap = FromBranchTap,
                HangerType = HangerType,
                HangerWithInsulation = HangerWithInsulation,
                PendingAction = PendingAction,
                ErrorMessage = ErrorMessage
            };
        }
    }
}