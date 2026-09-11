using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public static class HangerRuleValidator
    {
        public static bool TryValidate(
            Document document,
            IEnumerable<HangerRuleDefinition> definitions,
            IReadOnlyDictionary<string, HangerButtonOption> hangerButtons,
            out IReadOnlyList<ValidatedHangerRule> validatedRules,
            out string error)
        {
            List<ValidatedHangerRule> rules = new List<ValidatedHangerRule>();
            List<string> errors = new List<string>();

            foreach (HangerRuleDefinition definition in definitions.Where(rule => rule.Enabled))
            {
                string ruleLabel = string.IsNullOrWhiteSpace(definition.Name)
                    ? "Unnamed rule"
                    : definition.Name.Trim();

                if (!Enum.TryParse(
                    definition.SizeBasis,
                    true,
                    out HangerSizeBasis sizeBasis))
                {
                    errors.Add($"{ruleLabel}: choose a valid size basis.");
                    continue;
                }

                bool lengthsAreValid = true;
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.MinimumSize,
                    ruleLabel,
                    "minimum size",
                    errors,
                    out double minimumSize);
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.MaximumSize,
                    ruleLabel,
                    "maximum size",
                    errors,
                    out double maximumSize);
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.MaximumSpacing,
                    ruleLabel,
                    "maximum spacing",
                    errors,
                    out double maximumSpacing);
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.FromEnd,
                    ruleLabel,
                    "distance from end",
                    errors,
                    out double fromEnd);
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.FromJoint,
                    ruleLabel,
                    "distance from joint",
                    errors,
                    out double fromJoint);
                lengthsAreValid &= TryParseLength(
                    document,
                    definition.FromBranchTap,
                    ruleLabel,
                    "distance from branch/tap",
                    errors,
                    out double fromBranchTap);

                if (!lengthsAreValid)
                {
                    continue;
                }

                if (minimumSize < 0 || maximumSize < minimumSize)
                {
                    errors.Add($"{ruleLabel}: the size range is invalid.");
                }

                if (maximumSpacing <= 0)
                {
                    errors.Add($"{ruleLabel}: maximum spacing must be greater than zero.");
                }

                if (fromEnd <= 0 || fromJoint <= 0 || fromBranchTap <= 0)
                {
                    errors.Add($"{ruleLabel}: all boundary offsets must be greater than zero.");
                }

                if (!hangerButtons.TryGetValue(
                    definition.StandardHangerKey ?? string.Empty,
                    out HangerButtonOption? standardHanger))
                {
                    errors.Add($"{ruleLabel}: choose a standard ITM hanger type.");
                    continue;
                }

                bool useStandardForInsulation = string.Equals(
                    definition.InsulatedHangerKey,
                    HangerPlacementConstants.SameAsStandardKey,
                    StringComparison.OrdinalIgnoreCase);

                HangerButtonReference? insulatedHanger = null;
                if (!useStandardForInsulation)
                {
                    if (!hangerButtons.TryGetValue(
                        definition.InsulatedHangerKey ?? string.Empty,
                        out HangerButtonOption? insulatedOption))
                    {
                        errors.Add(
                            $"{ruleLabel}: choose an insulated hanger type or 'Same as standard'.");
                        continue;
                    }

                    insulatedHanger = insulatedOption.ToReference();
                }

                rules.Add(
                    new ValidatedHangerRule(
                        definition.Id,
                        ruleLabel,
                        definition.Priority,
                        sizeBasis,
                        definition.ServiceKey ?? HangerPlacementConstants.AnyKey,
                        definition.MaterialKey ?? HangerPlacementConstants.AnyKey,
                        minimumSize,
                        maximumSize,
                        maximumSpacing,
                        fromEnd,
                        fromJoint,
                        fromBranchTap,
                        standardHanger.ToReference(),
                        insulatedHanger,
                        useStandardForInsulation));
            }

            if (!definitions.Any(rule => rule.Enabled))
            {
                errors.Add("Enable at least one hanger rule.");
            }

            if (errors.Count > 0)
            {
                validatedRules = Array.Empty<ValidatedHangerRule>();
                error = string.Join(Environment.NewLine, errors.Take(12));
                return false;
            }

            validatedRules = rules
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            error = string.Empty;
            return true;
        }

        private static bool TryParseLength(
            Document document,
            string text,
            string ruleName,
            string fieldName,
            ICollection<string> errors,
            out double value)
        {
            bool parsed = UnitFormatUtils.TryParse(
                document.GetUnits(),
                SpecTypeId.Length,
                text ?? string.Empty,
                out value,
                out string parseMessage);

            if (!parsed)
            {
                string detail = string.IsNullOrWhiteSpace(parseMessage)
                    ? "enter a valid Revit length"
                    : parseMessage;
                errors.Add($"{ruleName}: {fieldName} — {detail}.");
            }

            return parsed;
        }
    }
}
