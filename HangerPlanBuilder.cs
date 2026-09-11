using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public static class HangerPlanBuilder
    {
        private const double HorizontalTolerance = 1.0 / (64.0 * 12.0);
        private const double DuplicateTolerance = 1.0 / (8.0 * 12.0);

        private enum BoundaryKind
        {
            End,
            Joint,
            BranchTap
        }

        public static HangerPlacementPlan Build(
            Document document,
            IEnumerable<ElementId> selectedIds,
            HangerPlacementRequest request)
        {
            HangerPlacementPlan plan = new HangerPlacementPlan();
            FabricationConfiguration configuration =
                FabricationConfiguration.GetFabricationConfiguration(document);

            Dictionary<ElementId, List<XYZ>> existingHangers =
                IndexExistingHangers(document);

            List<ElementId> uniqueIds = selectedIds.Distinct().ToList();
            plan.HostCount = uniqueIds.Count;

            foreach (ElementId id in uniqueIds)
            {
                if (document.GetElement(id) is not FabricationPart part)
                {
                    plan.Skipped.Add($"Element {id.Value}: not a fabrication part.");
                    continue;
                }

                string partLabel = $"Element {part.Id.Value}";

                if (part.IsAHanger())
                {
                    plan.Skipped.Add($"{partLabel}: existing hanger selected as a host.");
                    continue;
                }

                if (!part.IsAStraight())
                {
                    plan.Skipped.Add($"{partLabel}: only straight fabrication segments are supported.");
                    continue;
                }

                List<Connector> connectors = GetPhysicalEndConnectors(part);
                if (connectors.Count != 2)
                {
                    plan.Skipped.Add($"{partLabel}: expected two physical end connectors.");
                    continue;
                }

                Connector startConnector = connectors[0];
                Connector endConnector = connectors[1];
                XYZ axisVector = endConnector.Origin - startConnector.Origin;
                double connectorLength = axisVector.GetLength();

                if (connectorLength <= HorizontalTolerance)
                {
                    plan.Skipped.Add($"{partLabel}: segment length could not be determined.");
                    continue;
                }

                XYZ direction = axisVector.Normalize();
                double slopeAngle = Math.Asin(
                    Math.Min(1.0, Math.Abs(direction.Z)));

                if (slopeAngle > document.Application.AngleTolerance)
                {
                    plan.Skipped.Add($"{partLabel}: sloped or vertical segments are not supported yet.");
                    continue;
                }

                string serviceKey =
                    FabricationCatalogService.GetServiceKey(configuration, part);
                string materialKey =
                    FabricationCatalogService.GetMaterialKey(configuration, part);

                List<ValidatedHangerRule> matchingRules = request.Rules
                    .Where(rule => RuleMatches(
                        rule,
                        serviceKey,
                        materialKey,
                        startConnector))
                    .OrderBy(rule => rule.Priority)
                    .ToList();

                if (matchingRules.Count == 0)
                {
                    plan.Skipped.Add($"{partLabel}: no enabled rule matches its service, material, and size.");
                    continue;
                }

                int winningPriority = matchingRules[0].Priority;
                List<ValidatedHangerRule> winningRules = matchingRules
                    .Where(rule => rule.Priority == winningPriority)
                    .ToList();

                if (winningRules.Count > 1)
                {
                    plan.Skipped.Add(
                        $"{partLabel}: multiple rules match at priority {winningPriority}.");
                    continue;
                }

                ValidatedHangerRule rule = winningRules[0];
                HangerButtonReference? hangerButton = part.HasInsulation
                    ? GetInsulatedHanger(rule)
                    : rule.StandardHanger;

                if (hangerButton == null)
                {
                    plan.Skipped.Add($"{partLabel}: its matching rule has no insulated hanger type.");
                    continue;
                }

                double segmentLength = part.CenterlineLength;
                if (segmentLength <= HorizontalTolerance)
                {
                    segmentLength = connectorLength;
                }

                double startOffset = GetBoundaryOffset(
                    rule,
                    ClassifyBoundary(part, startConnector));
                double endOffset = GetBoundaryOffset(
                    rule,
                    ClassifyBoundary(part, endConnector));

                double left = startOffset;
                double right = segmentLength - endOffset;

                if (left > right + HorizontalTolerance)
                {
                    plan.Skipped.Add(
                        $"{partLabel}: too short for its two configured boundary offsets.");
                    continue;
                }

                List<double> positions = BuildPositions(
                    left,
                    right,
                    rule.MaximumSpacing);

                int duplicateCount = 0;
                List<XYZ> existingOrigins = existingHangers.TryGetValue(
                    part.Id,
                    out List<XYZ>? origins)
                    ? origins
                    : new List<XYZ>();

                List<double> acceptedPositions = new List<double>();

                foreach (double position in positions)
                {
                    bool duplicate = request.SkipExistingHangers &&
                        IsDuplicatePosition(
                            startConnector.Origin,
                            direction,
                            position,
                            existingOrigins,
                            acceptedPositions);

                    if (duplicate)
                    {
                        duplicateCount++;
                        continue;
                    }

                    acceptedPositions.Add(position);
                    plan.Items.Add(
                        new HangerPlanItem(
                            part.Id,
                            startConnector.Id,
                            position,
                            rule.Name,
                            hangerButton));
                }

                plan.ReadyHostCount++;

                if (duplicateCount > 0)
                {
                    plan.Skipped.Add(
                        $"{partLabel}: {duplicateCount} existing hanger position(s) skipped.");
                }
            }

            return plan;
        }

        private static HangerButtonReference? GetInsulatedHanger(
            ValidatedHangerRule rule)
        {
            if (rule.UseStandardHangerForInsulation)
            {
                return rule.StandardHanger;
            }

            return rule.InsulatedHanger;
        }

        private static bool RuleMatches(
            ValidatedHangerRule rule,
            string serviceKey,
            string materialKey,
            Connector sizeConnector)
        {
            bool serviceMatches = string.Equals(
                rule.ServiceKey,
                HangerPlacementConstants.AnyKey,
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    rule.ServiceKey,
                    serviceKey,
                    StringComparison.OrdinalIgnoreCase);

            bool materialMatches = string.Equals(
                rule.MaterialKey,
                HangerPlacementConstants.AnyKey,
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    rule.MaterialKey,
                    materialKey,
                    StringComparison.OrdinalIgnoreCase);

            if (!serviceMatches || !materialMatches)
            {
                return false;
            }

            double size = GetConnectorSize(sizeConnector, rule.SizeBasis);
            return size >= rule.MinimumSize - HorizontalTolerance &&
                size <= rule.MaximumSize + HorizontalTolerance;
        }

        private static double GetConnectorSize(
            Connector connector,
            HangerSizeBasis sizeBasis)
        {
            if (connector.Shape == ConnectorProfileType.Round)
            {
                double diameter = connector.Radius * 2.0;
                return sizeBasis == HangerSizeBasis.HalfPerimeter
                    ? Math.PI * connector.Radius
                    : diameter;
            }

            double width = connector.Width;
            double depth = connector.Height;

            return sizeBasis switch
            {
                HangerSizeBasis.Width => width,
                HangerSizeBasis.Depth => depth,
                HangerSizeBasis.HalfPerimeter => width + depth,
                HangerSizeBasis.LargestDimension => Math.Max(width, depth),
                _ => Math.Max(width, depth)
            };
        }

        private static List<double> BuildPositions(
            double left,
            double right,
            double maximumSpacing)
        {
            if (Math.Abs(right - left) <= HorizontalTolerance)
            {
                return new List<double> { left };
            }

            int intervalCount = Math.Max(
                1,
                (int)Math.Ceiling((right - left) / maximumSpacing));
            double actualSpacing = (right - left) / intervalCount;

            List<double> positions = new List<double>(intervalCount + 1);
            for (int index = 0; index <= intervalCount; index++)
            {
                positions.Add(left + (actualSpacing * index));
            }

            return positions;
        }

        private static BoundaryKind ClassifyBoundary(
            FabricationPart host,
            Connector connector)
        {
            List<Connector> externalConnections = new List<Connector>();

            foreach (Connector reference in connector.AllRefs)
            {
                if (reference.Owner.Id != host.Id && reference.IsConnected)
                {
                    externalConnections.Add(reference);
                }
            }

            if (externalConnections.Count == 0)
            {
                return BoundaryKind.End;
            }

            if (externalConnections.Count > 1)
            {
                return BoundaryKind.BranchTap;
            }

            foreach (Connector connection in externalConnections)
            {
                if (connection.Owner is FabricationPart connectedPart &&
                    (connectedPart.IsATap() ||
                     GetPhysicalEndConnectors(connectedPart).Count > 2))
                {
                    return BoundaryKind.BranchTap;
                }
            }

            return BoundaryKind.Joint;
        }

        private static double GetBoundaryOffset(
            ValidatedHangerRule rule,
            BoundaryKind boundaryKind)
        {
            return boundaryKind switch
            {
                BoundaryKind.End => rule.FromEnd,
                BoundaryKind.Joint => rule.FromJoint,
                BoundaryKind.BranchTap => rule.FromBranchTap,
                _ => Math.Max(rule.FromJoint, rule.FromBranchTap)
            };
        }

        private static List<Connector> GetPhysicalEndConnectors(
            FabricationPart part)
        {
            List<Connector> connectors = new List<Connector>();

            foreach (Connector connector in part.ConnectorManager.Connectors)
            {
                if (connector.ConnectorType == ConnectorType.End)
                {
                    connectors.Add(connector);
                }
            }

            return connectors;
        }

        private static Dictionary<ElementId, List<XYZ>> IndexExistingHangers(
            Document document)
        {
            Dictionary<ElementId, List<XYZ>> result =
                new Dictionary<ElementId, List<XYZ>>();

            IEnumerable<FabricationPart> hangers =
                new FilteredElementCollector(document)
                    .OfClass(typeof(FabricationPart))
                    .Cast<FabricationPart>()
                    .Where(part => part.IsAHanger());

            foreach (FabricationPart hanger in hangers)
            {
                try
                {
                    FabricationHostedInfo? hostedInfo = hanger.GetHostedInfo();
                    if (hostedInfo == null)
                    {
                        continue;
                    }

                    using (hostedInfo)
                    {
                        if (!result.TryGetValue(
                            hostedInfo.HostId,
                            out List<XYZ>? origins))
                        {
                            origins = new List<XYZ>();
                            result.Add(hostedInfo.HostId, origins);
                        }

                        origins.Add(hanger.Origin);
                    }
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException)
                {
                    // Free-placed hangers have no usable hosted information.
                }
            }

            return result;
        }

        private static bool IsDuplicatePosition(
            XYZ start,
            XYZ direction,
            double candidateDistance,
            IEnumerable<XYZ> existingOrigins,
            IEnumerable<double> acceptedPositions)
        {
            foreach (XYZ origin in existingOrigins)
            {
                double existingDistance = (origin - start).DotProduct(direction);
                if (Math.Abs(existingDistance - candidateDistance) <= DuplicateTolerance)
                {
                    return true;
                }
            }

            return acceptedPositions.Any(
                distance => Math.Abs(distance - candidateDistance) <= DuplicateTolerance);
        }
    }
}
