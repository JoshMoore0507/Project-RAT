using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace TPMechanical.QAQC
{
    public class TPMechanicalApp : IExternalApplication
    {
        public Result OnStartup(
            UIControlledApplication application)
        {
            string tabName = "TP Mechanical";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // TP Mechanical tab already exists.
            }

            string assemblyPath =
                Assembly.GetExecutingAssembly().Location;


            // ========================================
            // QA/QC PANEL
            // ========================================

            RibbonPanel qaPanel =
                application.CreateRibbonPanel(
                    tabName,
                    "QA/QC");

            PushButtonData qaButtonData =
                new PushButtonData(
                    "TP_QAQC",
                    "Drawing\nQA/QC",
                    assemblyPath,
                    "TPMechanical.QAQC.QAQCCommand");

            PushButton qaButton =
                qaPanel.AddItem(qaButtonData)
                as PushButton;

            if (qaButton != null)
            {
                qaButton.ToolTip =
                    "Run TP Mechanical drawing QA/QC checks.";
            }


            // ========================================
            // SUPPORTS PANEL
            // ========================================

            RibbonPanel supportsPanel =
                application.CreateRibbonPanel(
                    tabName,
                    "Supports");

            PushButtonData hangerButtonData =
                new PushButtonData(
                    "TP_HangerPlacement",
                    "Hanger\nPlacement",
                    assemblyPath,
                    "TPMechanical.QAQC.HangerPlacementCommand");

            PushButton hangerButton =
                supportsPanel.AddItem(hangerButtonData)
                as PushButton;

            if (hangerButton != null)
            {
                // Small description shown when hovering.
                hangerButton.ToolTip =
                    "Automatically configure and place hangers using TP Mechanical standards.";

                // Expanded description shown after hovering.
                hangerButton.LongDescription =
                    "TP Mechanical Hanger Placement automates hanger layout for fabrication pipe and duct. " +
                    "It uses TP Mechanical rules for service, material, size, spacing, end offsets, " +
                    "hanger type, collision adjustment, structural attachment, and layout point creation.";

                // Load hanger icon.
                BitmapImage? hangerIcon =
                    LoadRibbonImage(
                        "Resources/HangerPlacement32.png");

                if (hangerIcon != null)
                {
                    hangerButton.LargeImage =
                        hangerIcon;
                }
            }


            return Result.Succeeded;
        }


        // ========================================
        // IMAGE LOADER
        // ========================================

        private static BitmapImage? LoadRibbonImage(
            string resourcePath)
        {
            try
            {
                string assemblyName =
                    Assembly.GetExecutingAssembly()
                        .GetName()
                        .Name!;

                Uri uri =
                    new Uri(
                        $"pack://application:,,,/{assemblyName};component/{resourcePath}",
                        UriKind.Absolute);

                BitmapImage image =
                    new BitmapImage();

                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption =
                    BitmapCacheOption.OnLoad;
                image.EndInit();

                image.Freeze();

                return image;
            }
            catch
            {
                // If the image cannot be found,
                // still allow the TP Mechanical add-in to load.
                return null;
            }
        }


        // ========================================
        // SHUTDOWN
        // ========================================

        public Result OnShutdown(
            UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}