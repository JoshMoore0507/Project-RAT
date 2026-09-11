using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace TPMechanical.QAQC
{
    public sealed class FabricationCatalogSnapshot
    {
        public List<CatalogOption> Services { get; } = new List<CatalogOption>();

        public List<CatalogOption> Materials { get; } = new List<CatalogOption>();

        public List<HangerButtonOption> HangerButtons { get; } = new List<HangerButtonOption>();
    }

    public sealed class ResolvedHangerButton : IDisposable
    {
        public ResolvedHangerButton(
            FabricationService service,
            FabricationServiceButton button)
        {
            Service = service;
            Button = button;
        }

        public FabricationService Service { get; }

        public FabricationServiceButton Button { get; }

        public void Dispose()
        {
            Button.Dispose();
            Service.Dispose();
        }
    }

    public static class FabricationCatalogService
    {
        public static bool TryCreateSnapshot(
            Document document,
            out FabricationCatalogSnapshot snapshot,
            out string error)
        {
            snapshot = new FabricationCatalogSnapshot();
            error = string.Empty;

            try
            {
                FabricationConfiguration configuration =
                    FabricationConfiguration.GetFabricationConfiguration(document);

                if (configuration == null || !configuration.HasValidConfiguration())
                {
                    error = "This model does not have a valid fabrication configuration.";
                    return false;
                }

                HashSet<string> serviceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                IList<FabricationService> loadedServices = configuration.GetAllLoadedServices();

                foreach (FabricationService service in loadedServices)
                {
                    try
                    {
                        Guid serviceGuid = configuration.GetServiceGUID(service.ServiceId);
                        string serviceKey = serviceGuid.ToString("D");

                        if (serviceKeys.Add(serviceKey))
                        {
                            snapshot.Services.Add(new CatalogOption(serviceKey, service.Name));
                        }

                        AddHangerButtons(snapshot, service, serviceGuid);
                    }
                    finally
                    {
                        service.Dispose();
                    }
                }

                HashSet<int> materialIds = new HashSet<int>();
                IEnumerable<FabricationPart> modelParts =
                    new FilteredElementCollector(document)
                        .OfClass(typeof(FabricationPart))
                        .Cast<FabricationPart>();

                foreach (FabricationPart part in modelParts)
                {
                    if (!part.IsAHanger())
                    {
                        materialIds.Add(part.Material);
                    }
                }

                foreach (int materialId in materialIds)
                {
                    try
                    {
                        Guid materialGuid = configuration.GetMaterialGUID(materialId);
                        string materialName = configuration.GetMaterialName(materialId);
                        snapshot.Materials.Add(
                            new CatalogOption(materialGuid.ToString("D"), materialName));
                    }
                    catch (Autodesk.Revit.Exceptions.ApplicationException)
                    {
                        // Some database items do not expose a resolvable material.
                    }
                }

                snapshot.Services.Sort(
                    (left, right) => string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase));

                snapshot.Materials.Sort(
                    (left, right) => string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase));

                snapshot.HangerButtons.Sort(
                    (left, right) => string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase));

                if (snapshot.HangerButtons.Count == 0)
                {
                    error = "No loaded fabrication service contains an available ITM hanger button.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = "The fabrication catalog could not be read: " + exception.Message;
                return false;
            }
        }

        public static ResolvedHangerButton ResolveButton(
            Document document,
            HangerButtonReference reference)
        {
            FabricationConfiguration configuration =
                FabricationConfiguration.GetFabricationConfiguration(document);

            int serviceId = configuration.GetServiceByGUID(reference.ServiceGuid);
            FabricationService service = configuration.GetService(serviceId);

            try
            {
                FabricationServiceButton? snapshotButton =
                    TryGetSnapshotButton(service, reference);

                if (snapshotButton != null)
                {
                    return new ResolvedHangerButton(service, snapshotButton);
                }

                for (int paletteIndex = 0; paletteIndex < service.PaletteCount; paletteIndex++)
                {
                    string paletteName = service.GetPaletteName(paletteIndex);
                    if (!string.Equals(
                        paletteName,
                        reference.PaletteName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int buttonCount = service.GetButtonCount(paletteIndex);
                    for (int buttonIndex = 0; buttonIndex < buttonCount; buttonIndex++)
                    {
                        FabricationServiceButton button =
                            service.GetButton(paletteIndex, buttonIndex);

                        if (IsMatchingHangerButton(button, reference))
                        {
                            return new ResolvedHangerButton(service, button);
                        }

                        button.Dispose();
                    }
                }

                throw new InvalidOperationException(
                    $"Hanger type '{reference.ButtonName}' is no longer available in service '{reference.ServiceName}'.");
            }
            catch
            {
                service.Dispose();
                throw;
            }
        }

        public static string GetServiceKey(
            FabricationConfiguration configuration,
            FabricationPart part)
        {
            return configuration.GetServiceGUID(part.ServiceId).ToString("D");
        }

        public static string GetMaterialKey(
            FabricationConfiguration configuration,
            FabricationPart part)
        {
            return configuration.GetMaterialGUID(part.Material).ToString("D");
        }

        private static void AddHangerButtons(
            FabricationCatalogSnapshot snapshot,
            FabricationService service,
            Guid serviceGuid)
        {
            for (int paletteIndex = 0; paletteIndex < service.PaletteCount; paletteIndex++)
            {
                string paletteName = service.GetPaletteName(paletteIndex);
                int buttonCount = service.GetButtonCount(paletteIndex);

                for (int buttonIndex = 0; buttonIndex < buttonCount; buttonIndex++)
                {
                    using FabricationServiceButton button =
                        service.GetButton(paletteIndex, buttonIndex);

                    if (!button.IsValidObject ||
                        !button.IsValid() ||
                        !button.IsAHanger)
                    {
                        continue;
                    }

                    string key = string.Join(
                        "|",
                        serviceGuid.ToString("D"),
                        paletteName,
                        button.Code,
                        button.Name);

                    string displayName =
                        $"{service.Name} — {paletteName} — {button.Name}";

                    snapshot.HangerButtons.Add(
                        new HangerButtonOption(
                            key,
                            displayName,
                            serviceGuid,
                            service.Name,
                            paletteName,
                            button.Code,
                            button.Name,
                            paletteIndex,
                            buttonIndex));
                }
            }
        }

        private static FabricationServiceButton? TryGetSnapshotButton(
            FabricationService service,
            HangerButtonReference reference)
        {
            if (!service.IsValidPaletteIndex(reference.PaletteIndex) ||
                !service.IsValidButtonIndex(reference.PaletteIndex, reference.ButtonIndex))
            {
                return null;
            }

            FabricationServiceButton button =
                service.GetButton(reference.PaletteIndex, reference.ButtonIndex);

            if (IsMatchingHangerButton(button, reference))
            {
                return button;
            }

            button.Dispose();
            return null;
        }

        private static bool IsMatchingHangerButton(
            FabricationServiceButton button,
            HangerButtonReference reference)
        {
            return button.IsValidObject &&
                button.IsValid() &&
                button.IsAHanger &&
                string.Equals(
                    button.Code,
                    reference.ButtonCode,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    button.Name,
                    reference.ButtonName,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
