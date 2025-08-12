using VoidX.WPF;

namespace Cavernize.Logic.External;

/// <summary>
/// Download required software.
/// </summary>
internal static class Downloader {
    /// <summary>
    /// Get the latest release build of a GitHub project if uploaded as release.
    /// </summary>
    /// <param name="repo">Username/repo</param>
    /// <param name="platform">Substring only matching the required release</param>
    /// <returns>A link to the downloadable build.</returns>
    public static string GetLatestGitHubVersion(string repo, string platform) {
        const string gitHubUrl = "https://github.com/";
        string releaseUrl = $"{gitHubUrl}{repo}/releases/latest";
        string latest = HTTP.GET(releaseUrl);

        string tagFinder = repo + "/releases/tag/";
        int latestTag = latest.IndexOf(tagFinder) + tagFinder.Length;
        int tagEnd = latest.IndexOf('"', latestTag);
        string tag = latest[latestTag..tagEnd];

        latest = HTTP.GET($"{gitHubUrl}{repo}/releases/expanded_assets/{tag}");

        int index;
        while ((index = latest.IndexOf(platform)) != -1) {
            int begin = latest.LastIndexOf('"', index) + 2;
            int end = latest.IndexOf('"', index);
            string link = latest[begin..end];
            if (link.StartsWith(repo)) {
                return gitHubUrl + link;
            }
        }
        throw new NoValidReleaseException();
    }

    /// <summary>
    /// Get a file from a <paramref name="url"/> and save it to a target <paramref name="path"/>.
    /// </summary>
    public static async Task Download(string url, string path, Action<double> progressReport) {
        string fileName = Path.GetFileName(path);
        const int bufferSize = 8192;
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) {
            throw new NetworkException($"Failed to download {fileName}. HTTP Status: {response.StatusCode}");
        }

        long? totalBytes = response.Content.Headers.ContentLength;
        using Stream contentStream = await response.Content.ReadAsStreamAsync();
        using FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);
        var buffer = new byte[bufferSize];
        long totalRead = 0;
        int bytesRead;
        DateTime nextUpdate = default;
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0) {
            await file.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;
            if (totalBytes.HasValue && (nextUpdate < DateTime.Now || totalRead == totalBytes)) {
                nextUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
                progressReport?.Invoke(totalRead / (double)totalBytes.Value);
            }
        }
    }
}
