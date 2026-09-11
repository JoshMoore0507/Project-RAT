using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;
using TPMechanical.HangerPlacement.Commands;
using TPMechanical.QAQC.Commands;

namespace TPMechanical.Addin;

public sealed class TPMechanicalApp : IExternalApplication
{
    private const string TabName = "TP Mechanical";

    public Result OnStartup(UIControlledApplication application)
    {
        TryCreateRibbonTab(application);
        CreateHangerPlacementButton(application);
        CreateQaqcButton(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private static void TryCreateRibbonTab(UIControlledApplication application)
    {
        try
        {
            application.CreateRibbonTab(TabName);
        }
        catch
        {
            // Revit throws when another TP Mechanical application already created the tab.
        }
    }

    private static void CreateHangerPlacementButton(UIControlledApplication application)
    {
        RibbonPanel supportsPanel = application.CreateRibbonPanel(TabName, "Supports");
        Assembly hangerAssembly = typeof(HangerPlacementCommand).Assembly;

        PushButtonData buttonData = new(
            "TP_HangerPlacement",
            "Hanger\nPlacement",
            hangerAssembly.Location,
            typeof(HangerPlacementCommand).FullName!);

        if (supportsPanel.AddItem(buttonData) is not PushButton button)
        {
            return;
        }

        button.ToolTip =
            "Automatically configure and place hangers using TP Mechanical standards.";

        button.LongDescription =
            "TP Mechanical Hanger Placement automates hanger layout for fabrication pipe and duct. " +
            "It uses TP Mechanical rules for service, material, size, spacing, end offsets, " +
            "hanger type, collision adjustment, structural attachment, and layout point creation.";

        button.LargeImage = LoadRibbonImage(
            hangerAssembly,
            "Resources/HangerPlacement32.png");
    }

    private static void CreateQaqcButton(UIControlledApplication application)
    {
        RibbonPanel qaqcPanel = application.CreateRibbonPanel(TabName, "QA/QC");
        Type commandType = typeof(QAQCCommand);

        PushButtonData buttonData = new(
            "TP_QAQC",
            "Drawing\nQA/QC",
            commandType.Assembly.Location,
            commandType.FullName!);

        if (qaqcPanel.AddItem(buttonData) is PushButton button)
        {
            button.ToolTip = "Run TP Mechanical drawing QA/QC checks.";
        }
    }

    private static BitmapImage? LoadRibbonImage(Assembly resourceAssembly, string resourcePath)
    {
        try
        {
            Uri uri = new(
                $"pack://application:,,,/{resourceAssembly.GetName().Name};component/{resourcePath}",
                UriKind.Absolute);

            BitmapImage image = new();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            // A missing icon must not prevent the add-in from loading.
            return null;
        }
    }
}
