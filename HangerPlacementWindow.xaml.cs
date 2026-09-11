using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public partial class HangerPlacementWindow : Window
    {
        private readonly Document _document;
        private readonly Dictionary<string, HangerButtonOption> _hangerButtonsByKey;
        private readonly HashSet<string> _serviceKeys;
        private readonly HashSet<string> _materialKeys;

        public HangerPlacementWindow(Document document)
        {
            _document = document;
            Rules = new ObservableCollection<HangerRuleDefinition>();
            Services = new ObservableCollection<CatalogOption>();
            Materials = new ObservableCollection<CatalogOption>();
            StandardHangerOptions = new ObservableCollection<CatalogOption>();
            InsulatedHangerOptions = new ObservableCollection<CatalogOption>();
            SizeBasisOptions = new ObservableCollection<string>(
                Enum.GetNames(typeof(HangerSizeBasis)));
            _hangerButtonsByKey = new Dictionary<string, HangerButtonOption>(
                StringComparer.OrdinalIgnoreCase);
            _serviceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _materialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            InitializeComponent();
            DataContext = this;

            LoadModelCatalog();
            LoadSavedProfile();
        }

        public ObservableCollection<HangerRuleDefinition> Rules { get; }

        public ObservableCollection<CatalogOption> Services { get; }

        public ObservableCollection<CatalogOption> Materials { get; }

        public ObservableCollection<CatalogOption> StandardHangerOptions { get; }

        public ObservableCollection<CatalogOption> InsulatedHangerOptions { get; }

        public ObservableCollection<string> SizeBasisOptions { get; }

        public HangerPlacementRequest? PlacementRequest { get; private set; }

        private void LoadModelCatalog()
        {
            Services.Add(new CatalogOption(HangerPlacementConstants.AnyKey, "Any service"));
            Materials.Add(new CatalogOption(HangerPlacementConstants.AnyKey, "Any material"));
            InsulatedHangerOptions.Add(
                new CatalogOption(
                    HangerPlacementConstants.SameAsStandardKey,
                    "Same as standard hanger"));

            if (!FabricationCatalogService.TryCreateSnapshot(
                _document,
                out FabricationCatalogSnapshot snapshot,
                out string error))
            {
                StatusTextBlock.Text = error;
                PlaceHangersButton.IsEnabled = false;
                SaveSettingsButton.IsEnabled = false;
                return;
            }

            foreach (CatalogOption service in snapshot.Services)
            {
                Services.Add(service);
                _serviceKeys.Add(service.Key);
            }

            foreach (CatalogOption material in snapshot.Materials)
            {
                Materials.Add(material);
                _materialKeys.Add(material.Key);
            }

            foreach (HangerButtonOption hangerButton in snapshot.HangerButtons)
            {
                _hangerButtonsByKey[hangerButton.Key] = hangerButton;
                StandardHangerOptions.Add(hangerButton);
                InsulatedHangerOptions.Add(hangerButton);
            }

            PlaceHangersButton.IsEnabled = true;
            SaveSettingsButton.IsEnabled = true;
            StatusTextBlock.Text =
                $"Ready — {snapshot.HangerButtons.Count} loaded ITM hanger type(s) found.";
        }

        private void LoadSavedProfile()
        {
            HangerPlacementSettingsStore.TryLoad(
                out HangerPlacementProfile profile,
                out string warning);

            ProfileNameTextBox.Text = profile.Name;
            AttachToStructureCheckBox.IsChecked = profile.AttachToStructure;
            SkipExistingHangersCheckBox.IsChecked = profile.SkipExistingHangers;

            foreach (HangerRuleDefinition rule in profile.Rules)
            {
                Rules.Add(rule);
            }

            if (Rules.Count == 0)
            {
                Rules.Add(HangerPlacementSettingsStore.CreateDefault().Rules[0]);
            }

            RulesGrid.SelectedIndex = 0;

            if (!string.IsNullOrWhiteSpace(warning))
            {
                StatusTextBlock.Text = warning;
            }
        }

        private void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            int nextPriority = Rules.Count == 0
                ? 100
                : Rules.Max(rule => rule.Priority) + 10;

            HangerRuleDefinition rule = new HangerRuleDefinition
            {
                Priority = nextPriority,
                Name = $"Rule {Rules.Count + 1}"
            };

            Rules.Add(rule);
            RulesGrid.SelectedItem = rule;
            RulesGrid.ScrollIntoView(rule);
            StatusTextBlock.Text = "New draft rule added. Save the profile to keep it.";
        }

        private void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (RulesGrid.SelectedItem is not HangerRuleDefinition selectedRule)
            {
                StatusTextBlock.Text = "Select a rule to duplicate.";
                return;
            }

            HangerRuleDefinition copy = selectedRule.Copy();
            Rules.Add(copy);
            RulesGrid.SelectedItem = copy;
            RulesGrid.ScrollIntoView(copy);
            StatusTextBlock.Text = "Rule duplicated. Save the profile to keep it.";
        }

        private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (RulesGrid.SelectedItem is not HangerRuleDefinition selectedRule)
            {
                StatusTextBlock.Text = "Select a rule to remove.";
                return;
            }

            Rules.Remove(selectedRule);
            StatusTextBlock.Text = "Rule removed. Save the profile to keep the change.";
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryCreateRequest(out _, out string validationError))
            {
                ShowValidationError(validationError);
                return;
            }

            if (!TrySaveProfile(out string saveError))
            {
                ShowValidationError(saveError);
                return;
            }

            StatusTextBlock.Text =
                "Profile saved to your TP Mechanical user settings.";
        }

        private void PlaceHangersButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryCreateRequest(
                out HangerPlacementRequest? request,
                out string validationError))
            {
                ShowValidationError(validationError);
                return;
            }

            if (!TrySaveProfile(out string saveError))
            {
                ShowValidationError(saveError);
                return;
            }

            PlacementRequest = request;
            DialogResult = true;
        }

        private bool TryCreateRequest(
            out HangerPlacementRequest? request,
            out string error)
        {
            request = null;

            List<string> staleFilters = new List<string>();
            foreach (HangerRuleDefinition rule in Rules.Where(item => item.Enabled))
            {
                if (!string.Equals(
                        rule.ServiceKey,
                        HangerPlacementConstants.AnyKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    !_serviceKeys.Contains(rule.ServiceKey))
                {
                    staleFilters.Add($"{rule.Name}: its saved fabrication service is not loaded.");
                }

                if (!string.Equals(
                        rule.MaterialKey,
                        HangerPlacementConstants.AnyKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    !_materialKeys.Contains(rule.MaterialKey))
                {
                    staleFilters.Add($"{rule.Name}: its saved material is not present in this model.");
                }
            }

            if (staleFilters.Count > 0)
            {
                error = string.Join(Environment.NewLine, staleFilters.Take(12));
                return false;
            }

            if (!HangerRuleValidator.TryValidate(
                _document,
                Rules,
                _hangerButtonsByKey,
                out IReadOnlyList<ValidatedHangerRule> validatedRules,
                out error))
            {
                return false;
            }

            request = new HangerPlacementRequest(
                validatedRules,
                AttachToStructureCheckBox.IsChecked == true,
                SkipExistingHangersCheckBox.IsChecked != false);
            return true;
        }

        private bool TrySaveProfile(out string error)
        {
            HangerPlacementProfile profile = new HangerPlacementProfile
            {
                Name = string.IsNullOrWhiteSpace(ProfileNameTextBox.Text)
                    ? "TP Standard"
                    : ProfileNameTextBox.Text.Trim(),
                AttachToStructure = AttachToStructureCheckBox.IsChecked == true,
                SkipExistingHangers = SkipExistingHangersCheckBox.IsChecked != false,
                Rules = Rules.ToList()
            };

            return HangerPlacementSettingsStore.TrySave(profile, out error);
        }

        private void ShowValidationError(string error)
        {
            StatusTextBlock.Text = "Review the highlighted profile settings.";
            MessageBox.Show(
                this,
                error,
                "TP Mechanical Hanger Placement",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
