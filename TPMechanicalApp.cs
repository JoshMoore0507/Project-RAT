using Autodesk.Revit.UI;
using System.Reflection;

namespace TPMechanical.QAQC
{
    public class TPMechanicalApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "TP Mechanical";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // The tab may already exist.
            }

            RibbonPanel panel =
                application.CreateRibbonPanel(tabName, "QA/QC");

            string assemblyPath =
                Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "TP_QAQC",
                "Drawing\nQA/QC",
                assemblyPath,
                "TPMechanical.QAQC.QAQCCommand");

            PushButton? button =
                panel.AddItem(buttonData) as PushButton;

            if (button != null)
            {
                button.ToolTip =
                    "Run TP Mechanical drawing QA/QC checks.";
            }

            RibbonPanel supportsPanel =
                application.CreateRibbonPanel(tabName, "Supports");

            PushButtonData hangerButtonData = new PushButtonData(
                "TP_HangerPlacement",
                "Hanger\nPlacement",
                assemblyPath,
                "TPMechanical.QAQC.HangerPlacementCommand");

            PushButton? hangerButton =
                supportsPanel.AddItem(hangerButtonData) as PushButton;

            if (hangerButton != null)
            {
                hangerButton.ToolTip =
                    "Configure rules and place ITM hangers on selected fabrication segments.";

                hangerButton.LongDescription =
                    "Rule-driven placement for selected straight, horizontal fabrication segments. " +
                    "Collision relocation, full-run traversal, trapezes, and layout points are planned for later phases.";
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
