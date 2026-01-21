using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using SEBotV2.API.Helpers;

namespace SEBotV2.API.Net
{
    public class UpdateManager
    {
        public class GitHubReleaseInfo
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            [JsonProperty("prerelease")]
            public bool PreRelease { get; set; }

            [JsonProperty("assets")]
            public GitHubAssetInfo[] Assets { get; set; }
        }

        public class GitHubAssetInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
        
        private static HttpClient _httpClient;
        private static string GithubRepo = "Mr-Baguetter/Engineers-Eye";
        private static string executableName = "SEBotV2.exe";

        public static async Task Init()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SEBotV2-Updater/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        public static async Task Stop()
        {
            _httpClient?.Dispose();
        }

        public static async Task<(Version, string)> CheckForUpdate(Version currentVersion, bool allowPreReleases = false)
        {
            try
            {
                LogManager.Info($"Current version: {currentVersion}. Checking for updates...");
                GitHubReleaseInfo latestRelease = await GetLatestReleaseAsync(allowPreReleases);
                
                if (latestRelease == null)
                {
                    LogManager.Warn("Could not fetch release information from GitHub.");
                    return (null, "Could not fetch release information from GitHub.");
                }

                string latestVersionTag = latestRelease.TagName?.TrimStart('v') ?? string.Empty;
                
                if (!Version.TryParse(latestVersionTag, out Version githubVersion))
                {
                    LogManager.Warn($"Could not parse version from tag: {latestRelease.TagName}");
                    return (null, $"Could not parse version from tag: {latestRelease.TagName}");
                }

                LogManager.Info($"Latest version: {githubVersion}");
                
                if (githubVersion > currentVersion)
                {
                    LogManager.Info("An update is available! Use the update command to install it.");
                    return (githubVersion, "An update is available! Use the update command to install it");
                }
                else if (githubVersion < currentVersion)
                {
                    LogManager.Info("You are on a Pre-Release or Developer version!");
                    return (null, "You are on a Pre-Release or Developer version!");
                }
                else
                {
                    LogManager.Info("You are on the latest version.");
                    return (null, "You are on the latest version");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error checking for updates: {ex.Message}");
                return (null, $"Error checking for updates: {ex.Message}");
            }
        }

        public static async Task<bool> DownloadUpdate(Version currentVersion, bool force = false, bool allowPreReleases = false)
        {
            try
            {
                GitHubReleaseInfo latestRelease = await GetLatestReleaseAsync(allowPreReleases);
                if (latestRelease == null)
                {
                    LogManager.Error("Could not fetch release information from GitHub.");
                    return false;
                }

                GitHubAssetInfo asset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(executableName, StringComparison.OrdinalIgnoreCase));
                if (asset == null || string.IsNullOrEmpty(asset.BrowserDownloadUrl))
                {
                    LogManager.Error($"Could not find the DLL ('{executableName}') in the latest GitHub release.");
                    return false;
                }

                string latestVersionTag = latestRelease.TagName?.TrimStart('v') ?? string.Empty;
                if (Version.TryParse(latestVersionTag, out Version latestGitHubVersion) && latestGitHubVersion <= currentVersion && !force)
                {
                    LogManager.Info("You are already on the latest version. Use force parameter to proceed anyway.");
                    return false;
                }

                LogManager.Info($"Downloading new version from {asset.BrowserDownloadUrl}...");
                byte[] fileBytes = await _httpClient.GetByteArrayAsync(asset.BrowserDownloadUrl);
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    LogManager.Error("Downloaded file is empty.");
                    return false;
                }

                LogManager.Info($"{executableName} downloaded successfully ({fileBytes.Length} bytes). Applying update...");

                string currentPath = Assembly.GetExecutingAssembly().Location;
                string directory = Path.GetDirectoryName(currentPath);
                string newFilePath = Path.Combine(directory, $"{executableName}.new");
                string oldFilePath = Path.Combine(directory, $"{executableName}.old");

                await File.WriteAllBytesAsync(newFilePath, fileBytes);

                LogManager.Info("Update downloaded. Restart the bot to apply the update.");
                LogManager.Info("The bot will replace the old executable on next startup.");

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to download update: {ex.Message}");
                return false;
            }
        }

        private static async Task<GitHubReleaseInfo> GetLatestReleaseAsync(bool allowPreReleases)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"https://api.github.com/repos/{GithubRepo}/releases");
                if (!response.IsSuccessStatusCode)
                {
                    LogManager.Error($"Failed to fetch release info from GitHub. Status: {response.StatusCode}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<GitHubReleaseInfo> releases = JsonConvert.DeserializeObject<List<GitHubReleaseInfo>>(jsonResponse);
                if (releases == null || releases.Count == 0)
                {
                    LogManager.Warn("No releases found on GitHub.");
                    return null;
                }

                IEnumerable<GitHubReleaseInfo> filtered = allowPreReleases ? releases : releases.Where(r => !r.PreRelease);
                GitHubReleaseInfo chosen = filtered.OrderByDescending(r =>
                {
                    string tag = r.TagName?.TrimStart('v') ?? string.Empty;
                    return Version.TryParse(tag, out Version v) ? v : new Version(0, 0);
                }).FirstOrDefault();

                return chosen;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error fetching GitHub releases: {ex.Message}");
                return null;
            }
        }
    }
}