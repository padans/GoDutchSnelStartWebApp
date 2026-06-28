using System;
using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart
{
    public static class SnelStartUploadSettingsLoader
    {
        private const string FileName = "SnelStartUploadSettings.json";
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SnelStartUploadSettingsLoader));

        private static string GetAppDataDirectory()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GoDutchMt940Tool");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Log.Debug("AppData-directory aangemaakt voor SnelStart instellingen: {Directory}.", dir);
            }

            return dir;
        }

        private static string GetAppDataPath()
        {
            return Path.Combine(GetAppDataDirectory(), FileName);
        }

        private static string GetLegacyPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
        }

        public static bool Exists()
        {
            var exists = File.Exists(GetAppDataPath()) || File.Exists(GetLegacyPath());
            Log.Debug("Controle op bestaan van SnelStart instellingen uitgevoerd. Exists: {Exists}.", exists);
            return exists;
        }

        public static SnelStartUploadSettings Load()
        {
            var appDataPath = GetAppDataPath();
            var legacyPath = GetLegacyPath();

            try
            {
                if (File.Exists(appDataPath))
                {
                    var json = File.ReadAllText(appDataPath);
                    var settings = JsonSerializer.Deserialize<SnelStartUploadSettings>(json)
                                   ?? new SnelStartUploadSettings();

                    Log.Information("SnelStart instellingen geladen vanuit AppData.");
                    return settings;
                }

                if (File.Exists(legacyPath))
                {
                    var json = File.ReadAllText(legacyPath);

                    var settings = JsonSerializer.Deserialize<SnelStartUploadSettings>(json)
                                   ?? new SnelStartUploadSettings();

                    Save(settings);

                    Log.Information("SnelStart instellingen geladen vanuit legacy pad en gemigreerd naar AppData.");
                    return settings;
                }

                var defaultSettings = new SnelStartUploadSettings();
                Save(defaultSettings);

                Log.Warning("SnelStart instellingenbestand niet gevonden; standaardinstellingen aangemaakt.");
                return defaultSettings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Laden van SnelStart instellingen mislukt.");
                throw;
            }
        }

        public static void Save(SnelStartUploadSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(GetAppDataPath(), json);

                Log.Information(
                    "SnelStart instellingen opgeslagen. AuthUrl aanwezig: {HasAuthUrl}, ApiBaseUrl aanwezig: {HasApiBaseUrl}, client key aanwezig: {HasClientKey}, subscription key aanwezig: {HasSubscriptionKey}.",
                    !string.IsNullOrWhiteSpace(settings.AuthUrl),
                    !string.IsNullOrWhiteSpace(settings.ApiBaseUrl),
                    !string.IsNullOrWhiteSpace(settings.ClientKey),
                    !string.IsNullOrWhiteSpace(settings.SubscriptionKey));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Opslaan van SnelStart instellingen mislukt.");
                throw;
            }
        }
    }
}
