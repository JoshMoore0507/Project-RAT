using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Interop;
using TPMechanical.HangerPlacement.Views;

namespace TPMechanical.HangerPlacement.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class HangerPlacementCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uiDoc =
                commandData.Application.ActiveUIDocument;

            HangerPlacementWindow window =
                new HangerPlacementWindow(uiDoc);

            WindowInteropHelper helper =
                new WindowInteropHelper(window);

            helper.Owner =
                commandData.Application.MainWindowHandle;

            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}
