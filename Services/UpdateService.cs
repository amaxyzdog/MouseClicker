using System.Net.Http;
using System.Text.Json;

namespace MouseClicker.Services;

/// <summary>基于 GitHub Releases 的自动更新（查询最新版本与历史发布）。</summary>
public static class UpdateService
{
    private const string RepoReleasesApi = "https://api.github.com/repos/amaxyzdog/MouseClicker/releases";
    public const string ReleasesPageUrl = "https://github.com/amaxyzdog/MouseClicker/releases";

    /// <summary>当前程序版本（取程序集版本主三段，如 1.0.0）。</summary>
    public static string CurrentVersion { get; } =
        typeof(UpdateService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    // 结果缓存：GitHub API 未认证限额 60 次/小时/IP，避免频繁检查触发限流
    private static IReadOnlyList<ReleaseInfo>? _cachedReleases;
    private static DateTime _cachedAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>查询最近的版本列表（新→旧）；网络失败时返回 null。结果缓存 10 分钟。</summary>
    public static async Task<IReadOnlyList<ReleaseInfo>?> GetReleasesAsync(int count = 10)
    {
        if (_cachedReleases is not null && DateTime.UtcNow - _cachedAt < CacheDuration)
        {
            return _cachedReleases;
        }

        try
        {
            using var client = CreateClient();
            using var resp = await client
                .GetAsync($"{RepoReleasesApi}?per_page={Math.Max(1, count)}")
                .ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var list = new List<ReleaseInfo>();
            foreach (var rel in doc.RootElement.EnumerateArray())
            {
                var tag = rel.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                var name = rel.TryGetProperty("name", out var n) ? n.GetString() : null;
                var body = rel.TryGetProperty("body", out var b) ? b.GetString() : null;
                var pageUrl = rel.TryGetProperty("html_url", out var h) ? h.GetString() : null;

                // 资产：完整安装包（MouseClickerSetup-*.exe）与无感更新包（MouseClicker-*.zip）
                string installerUrl = string.Empty;
                string zipUrl = string.Empty;
                if (rel.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        if (!asset.TryGetProperty("name", out var nameEl)
                            || !asset.TryGetProperty("browser_download_url", out var urlEl))
                        {
                            continue;
                        }

                        var assetName = nameEl.GetString();
                        var assetUrl = urlEl.GetString();
                        if (string.IsNullOrWhiteSpace(assetName) || string.IsNullOrWhiteSpace(assetUrl))
                        {
                            continue;
                        }

                        if (assetName.StartsWith("MouseClickerSetup", StringComparison.OrdinalIgnoreCase)
                            && assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            installerUrl = assetUrl;
                        }
                        else if (assetName.StartsWith("MouseClicker", StringComparison.OrdinalIgnoreCase)
                                 && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            zipUrl = assetUrl;
                        }
                    }
                }

                list.Add(new ReleaseInfo(
                    tag.TrimStart('v', 'V'),
                    installerUrl,
                    zipUrl,
                    name,
                    body,
                    pageUrl ?? ReleasesPageUrl));
            }

            _cachedReleases = list;
            _cachedAt = DateTime.UtcNow;
            return list;
        }
        catch
        {
            // 网络异常/接口不可用时视为获取失败
            return null;
        }
    }

    /// <summary>判断远端版本是否比当前版本新。</summary>
    public static bool IsNewer(ReleaseInfo release)
        => Version.TryParse(release.Version, out var remote)
           && Version.TryParse(CurrentVersion, out var local)
           && remote > local;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        // GitHub API 要求显式 User-Agent
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MouseClicker-Updater");
        return client;
    }
}

/// <summary>GitHub Release 信息（Version 为去掉 v 前缀的版本号；InstallerUrl 为安装包，ZipUrl 为无感更新包）。</summary>
public sealed record ReleaseInfo(
    string Version,
    string InstallerUrl,
    string ZipUrl,
    string? Name,
    string? Body,
    string ReleaseUrl);
