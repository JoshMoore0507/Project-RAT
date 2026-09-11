using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public static class HangerPlacementService
    {
        public static HangerPlacementResult Place(
            Document document,
            HangerPlacementPlan plan,
            HangerPlacementRequest request)
        {
            HangerPlacementResult result = new HangerPlacementResult
            {
                PlannedCount = plan.Items.Count
            };

            Dictionary<string, ResolvedHangerButton> resolvedButtons =
                new Dictionary<string, ResolvedHangerButton>(StringComparer.OrdinalIgnoreCase);

            using Transaction transaction =
                new Transaction(document, "TP Mechanical - Place Hangers");

            try
            {
                if (transaction.Start() != TransactionStatus.Started)
                {
                    throw new InvalidOperationException(
                        "Revit could not start the hanger placement transaction.");
                }

                foreach (HangerPlanItem item in plan.Items)
                {
                    using SubTransaction subTransaction = new SubTransaction(document);
                    bool isUnattached = false;

                    try
                    {
                        if (subTransaction.Start() != TransactionStatus.Started)
                        {
                            throw new InvalidOperationException(
                                "Revit could not start an item placement transaction.");
                        }

                        FabricationPart host =
                            document.GetElement(item.HostId) as FabricationPart
                            ?? throw new InvalidOperationException(
                                $"Host element {item.HostId.Value} is no longer available.");

                        Connector connector =
                            host.ConnectorManager.Lookup(item.StartConnectorId)
                            ?? throw new InvalidOperationException(
                                $"Host connector {item.StartConnectorId} is no longer available.");

                        ResolvedHangerButton resolvedButton = GetResolvedButton(
                            document,
                            item.HangerButton,
                            resolvedButtons);

                        FabricationPart hanger = FabricationPart.CreateHanger(
                            document,
                            resolvedButton.Button,
                            host.Id,
                            connector,
                            item.Distance,
                            request.AttachToStructure);

                        if (hanger == null)
                        {
                            throw new InvalidOperationException(
                                "Revit did not return a created hanger.");
                        }

                        if (request.AttachToStructure)
                        {
                            document.Regenerate();

                            try
                            {
                                FabricationRodInfo? rodInfo = hanger.GetRodInfo();
                                if (rodInfo == null)
                                {
                                    isUnattached = true;
                                }
                                else
                                {
                                    using (rodInfo)
                                    {
                                        isUnattached = !rodInfo.IsAttachedToStructure;
                                    }
                                }
                            }
                            catch (Autodesk.Revit.Exceptions.ApplicationException)
                            {
                                isUnattached = true;
                            }
                        }

                        if (subTransaction.Commit() != TransactionStatus.Committed)
                        {
                            throw new InvalidOperationException(
                                "Revit could not commit this hanger.");
                        }

                        result.CreatedCount++;

                        if (isUnattached)
                        {
                            result.UnattachedCount++;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.RegenerationFailedException)
                    {
                        RollBackIfStarted(subTransaction);
                        throw;
                    }
                    catch (Autodesk.Revit.Exceptions.ApplicationException exception)
                    {
                        RollBackIfStarted(subTransaction);
                        result.Failures.Add(
                            $"Host {item.HostId.Value} ({item.RuleName}): {exception.Message}");
                    }
                    catch (InvalidOperationException exception)
                    {
                        RollBackIfStarted(subTransaction);
                        result.Failures.Add(
                            $"Host {item.HostId.Value} ({item.RuleName}): {exception.Message}");
                    }
                }

                if (result.CreatedCount == 0)
                {
                    transaction.RollBack();
                }
                else if (transaction.Commit() != TransactionStatus.Committed)
                {
                    throw new InvalidOperationException(
                        "Revit could not commit the hanger placement batch.");
                }

                return result;
            }
            catch
            {
                if (transaction.GetStatus() == TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                throw;
            }
            finally
            {
                foreach (ResolvedHangerButton button in resolvedButtons.Values)
                {
                    button.Dispose();
                }
            }
        }

        private static ResolvedHangerButton GetResolvedButton(
            Document document,
            HangerButtonReference reference,
            IDictionary<string, ResolvedHangerButton> cache)
        {
            if (cache.TryGetValue(reference.Key, out ResolvedHangerButton? resolved))
            {
                return resolved;
            }

            resolved = FabricationCatalogService.ResolveButton(document, reference);
            cache.Add(reference.Key, resolved);
            return resolved;
        }

        private static void RollBackIfStarted(SubTransaction subTransaction)
        {
            if (subTransaction.GetStatus() == TransactionStatus.Started)
            {
                subTransaction.RollBack();
            }
        }
    }
}
