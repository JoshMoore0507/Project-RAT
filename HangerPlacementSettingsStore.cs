using System;
using System.IO;
using System.Text.Json;

namespace TPMechanical.QAQC
{
    public static class HangerPlacementSettingsStore
    {
        private const string SettingsFileName = "HangerPlacementProfile.json";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string SettingsPath
        {
            get
            {
                string applicationData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

                return Path.Combine(
                    applicationData,
                    "TP Mechanical",
                    SettingsFileName);
            }
        }

        public static bool TryLoad(out HangerPlacementProfile profile, out string warning)
        {
            profile = CreateDefault();
            warning = string.Empty;

            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return true;
                }

                string json = File.ReadAllText(SettingsPath);
                HangerPlacementProfile? loaded =
                    JsonSerializer.Deserialize<HangerPlacementProfile>(json, SerializerOptions);

                if (loaded == null ||
                    loaded.SchemaVersion != HangerPlacementConstants.ProfileSchemaVersion)
                {
                    warning = "The saved hanger profile is from an unsupported version. A new draft profile was loaded.";
                    return false;
                }

                loaded.Rules ??= new System.Collections.Generic.List<HangerRuleDefinition>();
                profile = loaded;
                return true;
            }
            catch (Exception exception)
            {
                warning = "The saved hanger profile could not be read: " + exception.Message;
                return false;
            }
        }

        public static bool TrySave(HangerPlacementProfile profile, out string error)
        {
            error = string.Empty;

            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    error = "The settings folder could not be determined.";
                    return false;
                }

                Directory.CreateDirectory(directory);

                string temporaryPath = SettingsPath + ".tmp";
                string json = JsonSerializer.Serialize(profile, SerializerOptions);
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, SettingsPath, true);
                return true;
            }
            catch (Exception exception)
            {
                error = "The hanger profile could not be saved: " + exception.Message;
                return false;
            }
        }

        public static HangerPlacementProfile CreateDefault()
        {
            return new HangerPlacementProfile
            {
                Rules = new System.Collections.Generic.List<HangerRuleDefinition>
                {
                    new HangerRuleDefinition
                    {
                        Name = "Default straight fabrication rule",
                        Priority = 100
                    }
                }
            };
        }
    }
}
