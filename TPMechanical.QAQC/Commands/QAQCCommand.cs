using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace TPMechanical.QAQC.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class QAQCCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uiDoc =
                commandData.Application.ActiveUIDocument;

            Document doc =
                uiDoc.Document;

            View activeView =
                doc.ActiveView;

            // Find all Fabrication Parts visible in the active view.
            List<FabricationPart> fabricationParts =
                new FilteredElementCollector(doc, activeView.Id)
                    .OfClass(typeof(FabricationPart))
                    .Cast<FabricationPart>()
                    .ToList();

            // Find all Independent Tags visible in the active view.
            List<IndependentTag> tags =
                new FilteredElementCollector(doc, activeView.Id)
                    .OfClass(typeof(IndependentTag))
                    .Cast<IndependentTag>()
                    .ToList();

            TaskDialog.Show(
                "TP Mechanical QA/QC",
                $"View: {activeView.Name}\n\n" +
                $"Fabrication Parts Found: {fabricationParts.Count}\n" +
                $"Tags Found: {tags.Count}");

            return Result.Succeeded;
        }
    }
}
