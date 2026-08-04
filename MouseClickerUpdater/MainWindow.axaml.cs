using System.Diagnostics;
using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MouseClickerUpdater;

/// <summary>
/// 独立更新程序：
/// --mode silent|full  --url <下载地址>  --version <新版本号>  --pid <旧进程ID>
/// silent：下载 Release 里的 .zip，解压后替换应用目录文件（不替换更新程序自身），再启动主程序；
/// full：下载安装包并启动它完成安装。
/// </summary>
public partial class MainWindow : Window
{
    private string _mode = "full";
    private string _url = string.Empty;
    private string _version = string.Empty;
    private int _pid;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        ParseArgs(args);
        VersionText.Text = string.IsNullOrWhiteSpace(_version) ? string.Empty : $"v{_version}";
        Opened += async (_, _) => await RunAsync();
    }

    private void ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (i + 1 >= args.Length)
            {
                break;
            }

            var value = args[i + 1];
            switch (args[i])
            {
                case "--mode":
                    _mode = value;
                    i++;
                    break;
                case "--url":
                    _url = value;
                    i++;
                    break;
                case "--version":
                    _version = value;
                    i++;
                    break;
                case "--pid" when int.TryParse(value, out var pid):
                    _pid = pid;
                    i++;
                    break;
            }
        }
    }

    private async Task RunAsync()
    {
        var progress = new Progress<double>(p => ProgressBar.Value = p * 100);
        try
        {
            if (string.IsNullOrWhiteSpace(_url))
            {
                StatusText.Text = "缺少下载地址，无法更新。";
                return;
            }

            if (_mode == "silent")
            {
                await RunSilentAsync(progress);
            }
            else
            {
                await RunFullAsync(progress);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "更新失败：" + ex.Message;
            DetailText.Text = "可到 GitHub Releases 页面手动下载更新。";
            ProgressBar.IsVisible = false;
        }
    }

    // ---- 无感安装：下载 zip → 等待旧程序退出 → 解压替换（不含更新程序）→ 重启主程序 ----

    private async Task RunSilentAsync(IProgress<double> progress)
    {
        DetailText.Text = "正在下载更新包…";
        var zipPath = await DownloadAsync(_url, progress);

        await WaitOldAppExitAsync();

        DetailText.Text = "正在解压并替换文件…";
        ProgressBar.IsIndeterminate = true;

        var extractDir = Path.Combine(Path.GetTempPath(), "MouseClickerUpdate", "extract");
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, true);
        }
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        CopyReplacing(extractDir, AppContext.BaseDirectory);

        DetailText.Text = "更新完成，正在启动…";
        LaunchApp();
        Close();
    }

    /// <summary>把源目录文件复制到应用目录（跳过更新程序自身）。</summary>
    private static void CopyReplacing(string sourceDir, string destDir)
    {
        const string selfExe = "MouseClickerUpdater.exe";
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(selfExe, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rel = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(destDir, rel);
            var targetDir = Path.GetDirectoryName(target);
            if (targetDir is not null)
            {
                Directory.CreateDirectory(targetDir);
            }
            File.Copy(file, target, overwrite: true);
        }
    }

    private void LaunchApp()
    {
        var appExe = Path.Combine(AppContext.BaseDirectory, "MouseClicker.exe");
        if (File.Exists(appExe))
        {
            Process.Start(new ProcessStartInfo { FileName = appExe, UseShellExecute = true });
        }
    }

    // ---- 完整安装：下载安装包 → 等待旧程序退出 → 启动安装程序 ----

    private async Task RunFullAsync(IProgress<double> progress)
    {
        DetailText.Text = "正在下载安装包…";
        var installer = await DownloadAsync(_url, progress);

        await WaitOldAppExitAsync();

        DetailText.Text = "正在启动安装程序…";
        Process.Start(new ProcessStartInfo { FileName = installer, UseShellExecute = true });
        Close();
    }

    // ---- 通用 ----

    /// <summary>
    /// 等待旧主程序退出，避免替换文件时被占用。
    /// 若用户设置了"不直接关闭程序"，主程序会继续运行，下载完成后需用户手动退出才能继续。
    /// </summary>
    private async Task WaitOldAppExitAsync()
    {
        if (_pid <= 0)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(_pid);
            if (process.HasExited)
            {
                return;
            }

            StatusText.Text = "更新包已就绪，正在等待程序退出…";
            process.EnableRaisingEvents = true;
            var exited = new TaskCompletionSource<bool>();
            process.Exited += (_, _) => exited.TrySetResult(true);
            var timeout = Task.Delay(TimeSpan.FromMinutes(5));
            if (await Task.WhenAny(exited.Task, timeout) != exited.Task)
            {
                throw new TimeoutException("等待程序退出超时，请手动退出鼠标连点器后重试");
            }
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch
        {
            // 进程已不存在，直接继续
        }
    }

    private static async Task<string> DownloadAsync(string url, IProgress<double> progress)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MouseClickerUpdate");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir,
            url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "update.zip" : "setup.exe");

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MouseClicker-Updater");
        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        long total = resp.Content.Headers.ContentLength ?? 0;

        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        var buffer = new byte[81920];
        long read = 0;
        int n;
        while ((n = await src.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, n));
            read += n;
            if (total > 0)
            {
                progress.Report((double)read / total);
            }
        }

        return dest;
    }
}
