using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization; // Ważne dla atrybutów

namespace PAMP
{
    // Główny kontener
    public class AppManifest
    {
        [JsonPropertyName("versions")]
        public ComponentVersions Versions { get; set; }
    }

    // Klasa dla sekcji "versions"
    public class ComponentVersions
    {
        [JsonPropertyName("pamp")]
        public string Pamp { get; set; } = "?.?.?";

        [JsonPropertyName("php")]
        public string Php { get; set; } = "?.?.?";

        [JsonPropertyName("apache")]
        public string Apache { get; set; } = "?.?.?";

        [JsonPropertyName("mariadb")]
        public string MariaDb { get; set; } = "?.?.?";

        [JsonPropertyName("phpmyadmin")]
        public string PhpMyAdmin { get; set; } = "?.?.?";
    }

    public static class ManifestLoader
    {
        public static AppManifest Load()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manifest.json");

            if (!File.Exists(path))
            {
                return new AppManifest { Versions = new ComponentVersions() };
            }

            try
            {
                string json = File.ReadAllText(path);
                var manifest = JsonSerializer.Deserialize<AppManifest>(json);
                return manifest ?? new AppManifest { Versions = new ComponentVersions() };
            }
            catch
            {
                return new AppManifest { Versions = new ComponentVersions() };
            }
        }
    }
}