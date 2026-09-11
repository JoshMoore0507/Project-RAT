using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public static class HangerPlacementConstants
    {
        public const string AnyKey = "*";
        public const string SameAsStandardKey = "@same";
        public const int ProfileSchemaVersion = 1;
    }

    public enum HangerSizeBasis
    {
        Width,
        Depth,
        HalfPerimeter,
        LargestDimension
    }

    public class CatalogOption
    {
        public CatalogOption(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public string Key { get; }

        public string DisplayName { get; }
    }

    public sealed class HangerButtonOption : CatalogOption
    {
        public HangerButtonOption(
            string key,
            string displayName,
            Guid serviceGuid,
            string serviceName,
            string paletteName,
            string buttonCode,
            string buttonName,
            int paletteIndex,
            int buttonIndex)
            : base(key, displayName)
        {
            ServiceGuid = serviceGuid;
            ServiceName = serviceName;
            PaletteName = paletteName;
            ButtonCode = buttonCode;
            ButtonName = buttonName;
            PaletteIndex = paletteIndex;
            ButtonIndex = buttonIndex;
        }

        public Guid ServiceGuid { get; }

        public string ServiceName { get; }

        public string PaletteName { get; }

        public string ButtonCode { get; }

        public string ButtonName { get; }

        public int PaletteIndex { get; }

        public int ButtonIndex { get; }

        public HangerButtonReference ToReference()
        {
            return new HangerButtonReference(
                Key,
                ServiceGuid,
                ServiceName,
                PaletteName,
                ButtonCode,
                ButtonName,
                PaletteIndex,
                ButtonIndex);
        }
    }

    public sealed record HangerButtonReference(
        string Key,
        Guid ServiceGuid,
        string ServiceName,
        string PaletteName,
        string ButtonCode,
        string ButtonName,
        int PaletteIndex,
        int ButtonIndex);

    public sealed class HangerRuleDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Enabled { get; set; } = true;

        public int Priority { get; set; } = 100;

        public string Name { get; set; } = "New rule";

        public string SizeBasis { get; set; } = HangerSizeBasis.Width.ToString();

        public string ServiceKey { get; set; } = HangerPlacementConstants.AnyKey;

        public string MaterialKey { get; set; } = HangerPlacementConstants.AnyKey;

        public string MinimumSize { get; set; } = "0\"";

        public string MaximumSize { get; set; } = "20'-0\"";

        public string MaximumSpacing { get; set; } = "10'-0\"";

        public string FromEnd { get; set; } = "1'-6\"";

        public string FromJoint { get; set; } = "6\"";

        public string FromBranchTap { get; set; } = "1'-6\"";

        public string StandardHangerKey { get; set; } = string.Empty;

        public string InsulatedHangerKey { get; set; } = HangerPlacementConstants.SameAsStandardKey;

        public HangerRuleDefinition Copy()
        {
            return new HangerRuleDefinition
            {
                Id = Guid.NewGuid(),
                Enabled = Enabled,
                Priority = Priority + 1,
                Name = Name + " Copy",
                SizeBasis = SizeBasis,
                ServiceKey = ServiceKey,
                MaterialKey = MaterialKey,
                MinimumSize = MinimumSize,
                MaximumSize = MaximumSize,
                MaximumSpacing = MaximumSpacing,
                FromEnd = FromEnd,
                FromJoint = FromJoint,
                FromBranchTap = FromBranchTap,
                StandardHangerKey = StandardHangerKey,
                InsulatedHangerKey = InsulatedHangerKey
            };
        }
    }

    public sealed class HangerPlacementProfile
    {
        public int SchemaVersion { get; set; } = HangerPlacementConstants.ProfileSchemaVersion;

        public string Name { get; set; } = "TP Standard";

        public bool AttachToStructure { get; set; } = true;

        public bool SkipExistingHangers { get; set; } = true;

        public List<HangerRuleDefinition> Rules { get; set; } = new List<HangerRuleDefinition>();
    }

    public sealed record ValidatedHangerRule(
        Guid Id,
        string Name,
        int Priority,
        HangerSizeBasis SizeBasis,
        string ServiceKey,
        string MaterialKey,
        double MinimumSize,
        double MaximumSize,
        double MaximumSpacing,
        double FromEnd,
        double FromJoint,
        double FromBranchTap,
        HangerButtonReference StandardHanger,
        HangerButtonReference? InsulatedHanger,
        bool UseStandardHangerForInsulation);

    public sealed record HangerPlacementRequest(
        IReadOnlyList<ValidatedHangerRule> Rules,
        bool AttachToStructure,
        bool SkipExistingHangers);

    public sealed record HangerPlanItem(
        ElementId HostId,
        int StartConnectorId,
        double Distance,
        string RuleName,
        HangerButtonReference HangerButton);

    public sealed class HangerPlacementPlan
    {
        public List<HangerPlanItem> Items { get; } = new List<HangerPlanItem>();

        public List<string> Skipped { get; } = new List<string>();

        public int HostCount { get; set; }

        public int ReadyHostCount { get; set; }

        public string BuildSummary(int maximumSkipLines = 12)
        {
            List<string> lines = new List<string>
            {
                $"Selected segments: {HostCount}",
                $"Eligible segments: {ReadyHostCount}",
                $"Hangers planned: {Items.Count}"
            };

            if (Skipped.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Skipped or noted:");
                lines.AddRange(Skipped.Take(maximumSkipLines).Select(item => "• " + item));

                if (Skipped.Count > maximumSkipLines)
                {
                    lines.Add($"• …and {Skipped.Count - maximumSkipLines} more");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public sealed class HangerPlacementResult
    {
        public int PlannedCount { get; set; }

        public int CreatedCount { get; set; }

        public int UnattachedCount { get; set; }

        public List<string> Failures { get; } = new List<string>();

        public string BuildSummary(int maximumFailureLines = 12)
        {
            List<string> lines = new List<string>
            {
                $"Hangers planned: {PlannedCount}",
                $"Hangers created: {CreatedCount}",
                $"Not attached to structure: {UnattachedCount}"
            };

            if (Failures.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Placement failures:");
                lines.AddRange(Failures.Take(maximumFailureLines).Select(item => "• " + item));

                if (Failures.Count > maximumFailureLines)
                {
                    lines.Add($"• …and {Failures.Count - maximumFailureLines} more");
                }
            }

            if (CreatedCount > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Use Revit Undo once to remove this entire placement batch.");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
