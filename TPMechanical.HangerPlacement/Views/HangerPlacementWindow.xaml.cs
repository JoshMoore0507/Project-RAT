using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TPMechanical.HangerPlacement.Models;
using TPMechanical.HangerPlacement.Rules;

namespace TPMechanical.HangerPlacement.Views
{
    public partial class HangerPlacementWindow : Window
    {
        // Revit document access
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;


        // ========================================
        // CONSTRUCTOR
        // ========================================

        public HangerPlacementWindow(UIDocument uiDoc)
        {
            InitializeComponent();

            _uiDoc = uiDoc;
            _doc = uiDoc.Document;


            // Load session-based TP hanger rules.
            HangerRulesGrid.ItemsSource =
                HangerRuleSession.Rules;


            // Standard TP dropdowns.
            TypeColumn.ItemsSource =
                HangerRuleOptions.Types;

            MaterialColumn.ItemsSource =
                HangerRuleOptions.Materials;

            SizeByColumn.ItemsSource =
                HangerRuleOptions.SizeByOptions;


            // Read actual fabrication information
            // from the current Revit model.
            LoadFabricationServices();

            LoadHangerButtonNames();
        }



        // ========================================
        // LOAD FABRICATION SERVICES
        // ========================================

        private void LoadFabricationServices()
        {
            List<string> serviceNames =
                new List<string>();


            // ANY is always available for TP rules.
            serviceNames.Add("ANY");


            try
            {
                FabricationConfiguration configuration =
                    FabricationConfiguration
                        .GetFabricationConfiguration(_doc);


                if (configuration != null)
                {
                    var loadedServices =
                        configuration.GetAllLoadedServices();


                    foreach (FabricationService service
                             in loadedServices)
                    {
                        if (!string.IsNullOrWhiteSpace(
                            service.Name))
                        {
                            serviceNames.Add(
                                service.Name);
                        }
                    }
                }
            }
            catch
            {
                // If there is no valid fabrication
                // configuration, ANY remains available.
            }


            serviceNames =
                serviceNames
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        x => x == "ANY" ? 0 : 1)
                    .ThenBy(x => x)
                    .ToList();


            ServiceColumn.ItemsSource =
                serviceNames;
        }



        // ========================================
        // LOAD ACTUAL HANGER BUTTON NAMES
        // ========================================

        private void LoadHangerButtonNames()
        {
            List<string> hangerNames =
                GetLoadedHangerButtonNames();


            if (hangerNames.Count > 0)
            {
                HangerTypeColumn.ItemsSource =
                    hangerNames;
            }
            else
            {
                // Fallback while testing models that
                // do not have hanger content loaded.
                HangerTypeColumn.ItemsSource =
                    HangerRuleOptions.HangerTypes;
            }
        }



        // ========================================
        // GET ALL LOADED FABRICATION HANGERS
        // ========================================

        private List<string> GetLoadedHangerButtonNames()
        {
            List<string> names =
                new List<string>();


            try
            {
                FabricationConfiguration configuration =
                    FabricationConfiguration
                        .GetFabricationConfiguration(_doc);


                if (configuration == null)
                {
                    return names;
                }


                var services =
                    configuration.GetAllLoadedServices();


                foreach (FabricationService service
                         in services)
                {
                    for (int paletteIndex = 0;
                         paletteIndex <
                         service.PaletteCount;
                         paletteIndex++)
                    {
                        int buttonCount =
                            service.GetButtonCount(
                                paletteIndex);


                        for (int buttonIndex = 0;
                             buttonIndex <
                             buttonCount;
                             buttonIndex++)
                        {
                            FabricationServiceButton button =
                                service.GetButton(
                                    paletteIndex,
                                    buttonIndex);


                            if (button != null &&
                                button.IsAHanger &&
                                !string.IsNullOrWhiteSpace(
                                    button.Name))
                            {
                                names.Add(
                                    button.Name);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Leave list empty if fabrication
                // configuration cannot be read.
            }


            return names
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }



        // ========================================
        // FIND A SPECIFIC FABRICATION HANGER
        // ========================================

        private FabricationServiceButton?
            FindHangerButton(
                string hangerName)
        {
            FabricationConfiguration configuration =
                FabricationConfiguration
                    .GetFabricationConfiguration(_doc);


            if (configuration == null)
            {
                return null;
            }


            var services =
                configuration.GetAllLoadedServices();


            foreach (FabricationService service
                     in services)
            {
                for (int paletteIndex = 0;
                     paletteIndex <
                     service.PaletteCount;
                     paletteIndex++)
                {
                    int buttonCount =
                        service.GetButtonCount(
                            paletteIndex);


                    for (int buttonIndex = 0;
                         buttonIndex <
                         buttonCount;
                         buttonIndex++)
                    {
                        FabricationServiceButton button =
                            service.GetButton(
                                paletteIndex,
                                buttonIndex);


                        if (!button.IsAHanger)
                        {
                            continue;
                        }


                        if (string.Equals(
                            button.Name,
                            hangerName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return button;
                        }
                    }
                }
            }


            return null;
        }


        // ========================================
        // PARSE REVIT LENGTH
        // ========================================

        private bool TryParseRevitLength(
            string text,
            out double length)
        {
            length = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return UnitFormatUtils.TryParse(
                _doc.GetUnits(),
                SpecTypeId.Length,
                text,
                out length);
        }


        // ========================================
        // PLACE HANGERS ON SELECTED STRAIGHT
        // ========================================

        private void PlaceHangers_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // ========================================
                // GET SELECTED TP RULE
                // ========================================

                if (HangerRulesGrid.SelectedItem
                    is not HangerRule selectedRule)
                {
                    MessageBox.Show(
                        "Select a hanger rule first.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }


                if (string.IsNullOrWhiteSpace(
                    selectedRule.HangerType))
                {
                    MessageBox.Show(
                        "The selected rule does not have a hanger type.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // READ SPACING FROM RULE
                // ========================================

                if (!TryParseRevitLength(
                    selectedRule.Spacing,
                    out double spacing))
                {
                    MessageBox.Show(
                        "TP Mechanical could not read the hanger spacing.\n\n" +
                        $"Spacing value: {selectedRule.Spacing}\n\n" +
                        "Example valid values:\n8'\n8'-0\"",
                        "Invalid Hanger Spacing",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                if (spacing <= 0)
                {
                    MessageBox.Show(
                        "Hanger spacing must be greater than zero.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // READ FROM END FROM RULE
                // ========================================

                if (!TryParseRevitLength(
                    selectedRule.FromEnd,
                    out double fromEnd))
                {
                    MessageBox.Show(
                        "TP Mechanical could not read the From End value.\n\n" +
                        $"From End value: {selectedRule.FromEnd}\n\n" +
                        "Example valid values:\n1'\n1'-0\"",
                        "Invalid From End",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                if (fromEnd < 0)
                {
                    MessageBox.Show(
                        "From End cannot be less than zero.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // GET REVIT SELECTION
                // ========================================

                ICollection<ElementId> selectedIds =
                    _uiDoc.Selection.GetElementIds();


                if (selectedIds.Count != 1)
                {
                    MessageBox.Show(
                        "Before opening Hanger Placement, select exactly one straight Fabrication Part.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }


                ElementId selectedId =
                    selectedIds.First();


                FabricationPart? hostPart =
                    _doc.GetElement(selectedId)
                    as FabricationPart;


                if (hostPart == null)
                {
                    MessageBox.Show(
                        "The selected element is not a Fabrication Part.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // VERIFY STRAIGHT PART
                // ========================================

                if (!hostPart.IsAStraight())
                {
                    MessageBox.Show(
                        "For this version, select one straight Fabrication Part.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // GET END CONNECTORS
                // ========================================

                List<Connector> connectors =
                    new List<Connector>();


                foreach (Connector connector
                         in hostPart.ConnectorManager.Connectors)
                {
                    if (connector.ConnectorType ==
                        ConnectorType.End)
                    {
                        connectors.Add(connector);
                    }
                }


                if (connectors.Count < 2)
                {
                    MessageBox.Show(
                        "TP Mechanical could not find two end connectors on the selected Fabrication Part.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                Connector startConnector =
                    connectors[0];

                Connector endConnector =
                    connectors[1];


                // ========================================
                // VERIFY HORIZONTAL PART
                // ========================================

                double verticalDifference =
                    Math.Abs(
                        startConnector.Origin.Z -
                        endConnector.Origin.Z);


                // Approximately 1/16"
                double horizontalTolerance =
                    1.0 / 192.0;


                if (verticalDifference >
                    horizontalTolerance)
                {
                    MessageBox.Show(
                        "The selected Fabrication Part is not horizontal.\n\n" +
                        "Select a horizontal straight for this version.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // GET STRAIGHT LENGTH
                // ========================================

                double partLength =
                    startConnector.Origin.DistanceTo(
                        endConnector.Origin);


                if (partLength <= 0)
                {
                    MessageBox.Show(
                        "TP Mechanical could not determine the length of the selected part.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                if ((fromEnd * 2) >= partLength)
                {
                    MessageBox.Show(
                        "This straight is too short for the selected From End setting.\n\n" +
                        $"From End: {selectedRule.FromEnd}",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // FIND ACTUAL FABRICATION HANGER
                // ========================================

                FabricationServiceButton? hangerButton =
                    FindHangerButton(
                        selectedRule.HangerType);


                if (hangerButton == null)
                {
                    MessageBox.Show(
                        "TP Mechanical could not find this hanger in the loaded fabrication services.\n\n" +
                        $"Requested hanger:\n{selectedRule.HangerType}",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // CALCULATE HANGER LOCATIONS
                // ========================================

                List<double> hangerDistances =
                    new List<double>();


                double maximumDistance =
                    partLength - fromEnd;


                double currentDistance =
                    fromEnd;


                while (currentDistance <=
                       maximumDistance)
                {
                    hangerDistances.Add(
                        currentDistance);

                    currentDistance +=
                        spacing;
                }


                if (hangerDistances.Count == 0)
                {
                    MessageBox.Show(
                        "TP Mechanical could not calculate any valid hanger locations.",
                        "TP Mechanical",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // ========================================
                // CREATE ALL HANGERS
                // ========================================

                List<ElementId> newHangerIds =
                    new List<ElementId>();


                using (Transaction transaction =
                    new Transaction(
                        _doc,
                        "TP Mechanical - Place Hangers"))
                {
                    transaction.Start();


                    foreach (double distance
                             in hangerDistances)
                    {
                        FabricationPart hanger =
                            FabricationPart.CreateHanger(
                                _doc,
                                hangerButton,
                                hostPart.Id,
                                startConnector,
                                distance,

                                // Keep structure attachment
                                // disabled while developing.
                                false);


                        newHangerIds.Add(
                            hanger.Id);
                    }


                    transaction.Commit();
                }


                // ========================================
                // SELECT NEW HANGERS
                // ========================================

                _uiDoc.Selection.SetElementIds(
                    newHangerIds);


                // ========================================
                // SUCCESS MESSAGE
                // ========================================

                MessageBox.Show(
                    "Hanger placement completed successfully.\n\n" +

                    $"Hangers Placed: {newHangerIds.Count}\n\n" +

                    $"Hanger Type:\n" +
                    $"{selectedRule.HangerType}\n\n" +

                    $"Spacing: {selectedRule.Spacing}\n" +
                    $"From End: {selectedRule.FromEnd}\n\n" +

                    $"Host Service:\n" +
                    $"{hostPart.ServiceName}",

                    "TP Mechanical",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "TP Mechanical could not place the hangers.\n\n" +
                    ex.Message,

                    "Hanger Placement Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



        // ========================================
        // ADD RULE
        // ========================================

        private void AddRule_Click(
            object sender,
            RoutedEventArgs e)
        {
            HangerRule newRule =
                new HangerRule
                {
                    Use = true,
                    Service = "ANY"
                };


            HangerRuleSession.Rules.Add(
                newRule);


            HangerRulesGrid.SelectedItem =
                newRule;


            HangerRulesGrid.ScrollIntoView(
                newRule);
        }



        // ========================================
        // DELETE RULE
        // ========================================

        private void DeleteRule_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (HangerRulesGrid.SelectedItem
                is HangerRule selectedRule)
            {
                HangerRuleSession.Rules.Remove(
                    selectedRule);
            }
        }



        // ========================================
        // DUPLICATE RULE
        // ========================================

        private void DuplicateRule_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (HangerRulesGrid.SelectedItem
                is not HangerRule selectedRule)
            {
                return;
            }


            HangerRule duplicate =
                selectedRule.Clone();


            int selectedIndex =
                HangerRuleSession.Rules.IndexOf(
                    selectedRule);


            int insertIndex =
                selectedIndex + 1;


            HangerRuleSession.Rules.Insert(
                insertIndex,
                duplicate);


            HangerRulesGrid.SelectedItem =
                duplicate;


            HangerRulesGrid.ScrollIntoView(
                duplicate);
        }



        // ========================================
        // MOVE RULE UP
        // ========================================

        private void MoveUp_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (HangerRulesGrid.SelectedItem
                is not HangerRule selectedRule)
            {
                return;
            }


            int currentIndex =
                HangerRuleSession.Rules.IndexOf(
                    selectedRule);


            if (currentIndex <= 0)
            {
                return;
            }


            HangerRuleSession.Rules.Move(
                currentIndex,
                currentIndex - 1);


            HangerRulesGrid.SelectedItem =
                selectedRule;
        }



        // ========================================
        // MOVE RULE DOWN
        // ========================================

        private void MoveDown_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (HangerRulesGrid.SelectedItem
                is not HangerRule selectedRule)
            {
                return;
            }


            int currentIndex =
                HangerRuleSession.Rules.IndexOf(
                    selectedRule);


            if (currentIndex < 0 ||
                currentIndex >=
                HangerRuleSession.Rules.Count - 1)
            {
                return;
            }


            HangerRuleSession.Rules.Move(
                currentIndex,
                currentIndex + 1);


            HangerRulesGrid.SelectedItem =
                selectedRule;
        }



        // ========================================
        // RESET DEFAULTS
        // ========================================

        private void ResetDefaults_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    "Reset all hanger rules to the TP Mechanical defaults?\n\n" +
                    "Any changes made during this Revit session will be lost.",

                    "Reset Hanger Rules",

                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }


            HangerRuleSession
                .ResetToDefaults();
        }



        // ========================================
        // CLOSE WINDOW
        // ========================================

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
