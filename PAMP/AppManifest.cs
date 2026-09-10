using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAMP;

public class AppManifest
{
    [JsonPropertyName("versions")]
    public ComponentVersions Versions { get; set; } = new();
}

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
    private static readonly string ManifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manifest.json");

    public static AppManifest Load()
    {
        if (!File.Exists(ManifestPath))
        {
            return new AppManifest();
        }

        try
        {
            string json = File.ReadAllText(ManifestPath);
            return JsonSerializer.Deserialize<AppManifest>(json) ?? new AppManifest();
        }
        catch
        {
            return new AppManifest();
        }
    }

    public static void Save(AppManifest manifest)
    {
        try
        {
            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ManifestPath, json);
        }
        catch { }
    }
}