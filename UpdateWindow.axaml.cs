using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MouseClicker.Services;

namespace MouseClicker;

/// <summary>检查更新窗口：可选定版本，展示标签与可展开的版本介绍；下载安装由独立的更新程序完成。</summary>
public partial class UpdateWindow : Window
{
    private readonly IReadOnlyList<ReleaseInfo> _releases;
    private ReleaseInfo? _selected;
    private bool _bodyExpanded;

    public UpdateWindow(IReadOnlyList<ReleaseInfo> releases)
    {
        InitializeComponent();
        _releases = releases;

        var latest = releases[0];
        StatusText.Text = UpdateService.IsNewer(latest)
            ? $"当前版本 {UpdateService.CurrentVersion}，最新版本 v{latest.Version}"
            : $"当前已是最新版本（{UpdateService.CurrentVersion}）";

        // 版本下拉：新 → 旧，默认选最新（只显示版本标签）
        VersionBox.ItemsSource = releases
            .Select(r => new VersionItem($"v{r.Version}", r))
            .ToList();
        VersionBox.SelectedIndex = 0;
        VersionBox.SelectionChanged += (_, _) => ShowSelected();
        ShowSelected();
    }

    /// <summary>根据当前选中的版本刷新标签、介绍与按钮。</summary>
    private void ShowSelected()
    {
        if (VersionBox.SelectedItem is not VersionItem { Release: { } release })
        {
            return;
        }

        _selected = release;
        TagTitleText.Text = $"v{release.Version}";
        BodyText.Text = string.IsNullOrWhiteSpace(release.Body) ? "（该版本暂无更新说明）" : release.Body;
        _bodyExpanded = false;
        ApplyBodyLimit();

        // 该版本提供哪种安装方式就显示对应按钮
        SilentButton.IsVisible = !string.IsNullOrEmpty(release.ZipUrl);
        FullButton.IsVisible = !string.IsNullOrEmpty(release.InstallerUrl);
    }

    private void ApplyBodyLimit()
    {
        BodyText.MaxLines = _bodyExpanded ? int.MaxValue : 4;
        BodyText.TextTrimming = _bodyExpanded ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        ExpandButton.Content = _bodyExpanded ? "收起" : "展开";
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void ExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        _bodyExpanded = !_bodyExpanded;
        ApplyBodyLimit();
    }

    private void OpenReleasesPage_Click(object? sender, RoutedEventArgs e) => OpenUrl(UpdateService.ReleasesPageUrl);

    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    /// <summary>无感更新：启动独立更新程序，下载 zip 解压替换（不含更新程序）。</summary>
    private void SilentUpdate_Click(object? sender, RoutedEventArgs e) => LaunchUpdater("silent", _selected?.ZipUrl);

    /// <summary>完整安装：启动独立更新程序，下载安装包进行安装。</summary>
    private void FullInstall_Click(object? sender, RoutedEventArgs e) => LaunchUpdater("full", _selected?.InstallerUrl);

    private void LaunchUpdater(string mode, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        // 防止重复启动更新程序
        if (Process.GetProcessesByName("MouseClickerUpdater").Length > 0)
        {
            StatusText.Text = "更新程序已在运行，请稍候。";
            return;
        }

        var updaterPath = Path.Combine(AppContext.BaseDirectory, "MouseClickerUpdater.exe");
        if (!File.Exists(updaterPath))
        {
            StatusText.Text = "未找到更新程序（MouseClickerUpdater.exe），请重新安装后重试。";
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = updaterPath,
            UseShellExecute = true,
            Arguments = $"--mode {mode} --url \"{url}\" --version {_selected!.Version} --pid {Environment.ProcessId}",
        });

        // 更新程序接管后续流程（下载 → 替换/安装 → 重启新版），关闭本程序
        CloseAll();
    }

    /// <summary>关闭主窗口以退出程序（主窗口 Closed 时保存配置并释放资源），随后关闭本窗口。</summary>
    private void CloseAll()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow?.Close();
        }
        Close();
    }
}

/// <summary>版本下拉项。</summary>
internal sealed record VersionItem(string DisplayText, ReleaseInfo Release);
