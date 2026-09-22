using System.Text.Json.Serialization;

namespace PAMP;

public class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAssetDto> Assets { get; set; } = [];
}

public class GitHubAssetDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
}

public class UpdatePackageInfo
{
    public required string Version { get; init; }
    public required string Title { get; init; }
    public required string Changelog { get; init; }
    public required string ReleaseUrl { get; init; }
    public required string SelectedAssetUrl { get; init; }
    public required string SelectedAssetName { get; init; }
    public required long SizeBytes { get; init; }
    public required bool IsInstaller { get; init; }
    public required bool IsSelfContained { get; init; }
}
