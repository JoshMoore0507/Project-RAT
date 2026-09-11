using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace TPMechanical.QAQC
{
    [Transaction(TransactionMode.Manual)]
    public class HangerPlacementCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIDocument uiDocument = commandData.Application.ActiveUIDocument;
                Document document = uiDocument.Document;

                if (document.IsReadOnly)
                {
                    TaskDialog.Show(
                        "TP Mechanical Hanger Placement",
                        "The active model is read-only. Open an editable model before placing hangers.");
                    return Result.Cancelled;
                }

                HangerPlacementWindow window =
                    new HangerPlacementWindow(document);

                WindowInteropHelper windowHelper = new WindowInteropHelper(window)
                {
                    Owner = commandData.Application.MainWindowHandle
                };

                if (window.ShowDialog() != true ||
                    window.PlacementRequest == null)
                {
                    return Result.Cancelled;
                }

                HangerPlacementRequest request = window.PlacementRequest;
                List<ElementId> selectedIds = GetSelectedHostIds(
                    uiDocument,
                    document);

                if (selectedIds.Count == 0)
                {
                    return Result.Cancelled;
                }

                uiDocument.Selection.SetElementIds(selectedIds);

                HangerPlacementPlan plan = HangerPlanBuilder.Build(
                    document,
                    selectedIds,
                    request);

                if (plan.Items.Count == 0)
                {
                    TaskDialog.Show(
                        "TP Mechanical Hanger Placement",
                        "No hangers are ready to place.\n\n" +
                        plan.BuildSummary());
                    return Result.Succeeded;
                }

                TaskDialog confirmation = new TaskDialog(
                    "TP Mechanical Hanger Placement")
                {
                    MainInstruction =
                        $"Place {plan.Items.Count} hanger(s) on {plan.ReadyHostCount} segment(s)?",
                    MainContent = plan.BuildSummary(),
                    CommonButtons =
                        TaskDialogCommonButtons.Ok |
                        TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Cancel
                };

                if (confirmation.Show() != TaskDialogResult.Ok)
                {
                    return Result.Cancelled;
                }

                HangerPlacementResult placementResult =
                    HangerPlacementService.Place(document, plan, request);

                TaskDialog.Show(
                    "TP Mechanical Hanger Placement",
                    placementResult.BuildSummary());

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                message =
                    "The Hanger Placement window could not be opened. " +
                    exception.Message;

                return Result.Failed;
            }
        }

        private static List<ElementId> GetSelectedHostIds(
            UIDocument uiDocument,
            Document document)
        {
            List<ElementId> preselectedIds = uiDocument.Selection
                .GetElementIds()
                .Where(id =>
                    document.GetElement(id) is FabricationPart part &&
                    !part.IsAHanger())
                .ToList();

            if (preselectedIds.Count > 0)
            {
                return preselectedIds;
            }

            IList<Reference> references = uiDocument.Selection.PickObjects(
                ObjectType.Element,
                new FabricationPartSelectionFilter(),
                "Select fabrication segments for TP hanger placement, then click Finish");

            return references
                .Select(reference => reference.ElementId)
                .Distinct()
                .ToList();
        }
    }
}
