using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace TPMechanical.QAQC
{
    public static class HangerRuleSession
    {
        public static ObservableCollection<HangerRule> Rules { get; }
            = new ObservableCollection<HangerRule>();

        static HangerRuleSession()
        {
            ResetToDefaults();
        }

        public static void ResetToDefaults()
        {
            Rules.Clear();

            foreach (HangerRule defaultRule in HangerRuleDefaults.GetDefaults())
            {
                Rules.Add(defaultRule.Clone());
            }
        }
    }
}
