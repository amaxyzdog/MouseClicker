using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using MouseClicker.Services;
using MouseClicker.ViewModels;

namespace MouseClicker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private SettingsWindow? _settingsWindow;
    private TrayIcon? _trayIcon;
    private bool _startupCheckDone;
    private bool _updateBusy;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Icon = LoadWindowIcon();
        AppIcon.Source = LoadBitmap();
        CreateTrayIcon();

        _viewModel.WarningRequested += ShowWarning;
        _viewModel.UpdateCheckRequested += () => _ = CheckForUpdatesAsync(manual: true);
        Closed += (_, _) =>
        {
            _viewModel.Settings.FlushSave();
            _viewModel.Dispose();
        };
        // 启动后静默检查更新：有新版本才提示（可在设置中关闭）
        Loaded += (_, _) =>
        {
            if (_startupCheckDone)
            {
                return;
            }
            _startupCheckDone = true;
            if (_viewModel.Settings.AutoCheckUpdate)
            {
                _ = CheckForUpdatesAsync();
            }
        };
    }

    // ---- 图标资源 ----

    private static Bitmap? LoadBitmap()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://MouseClicker/icon.png"));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static WindowIcon? LoadWindowIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://MouseClicker/icon.png"));
            return new WindowIcon(new Bitmap(stream));
        }
        catch
        {
            return null;
        }
    }

    // ---- 托盘 ----

    private void CreateTrayIcon()
    {
        try
        {
            var icon = LoadWindowIcon();
            if (icon is null)
            {
                return;
            }

            var menu = new NativeMenu();
            var toggleItem = new NativeMenuItem("显示/隐藏主窗口");
            toggleItem.Click += (_, _) => ToggleShowHide();
            var startItem = new NativeMenuItem("启动/停止连点");
            startItem.Click += (_, _) => _viewModel.ToggleCommand.Execute(null);
            var exitItem = new NativeMenuItem("退出");
            exitItem.Click += (_, _) => Exit();
            menu.Items.Add(toggleItem);
            menu.Items.Add(startItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "鼠标连点器",
                Menu = menu,
                IsVisible = true,
            };
            _trayIcon.Clicked += (_, _) => ToggleShowHide();
        }
        catch
        {
            // 托盘创建失败不影响主窗口使用
        }
    }

    private void ToggleShowHide()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    private void Exit()
    {
        _trayIcon?.Dispose();
        Close();
    }

    // ---- 弹窗（替换 WPF 的 MessageBox） ----

    private async void ShowWarning(string message) => await ShowMessageAsync("连点器", message);

    private async Task ShowMessageAsync(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
            };
            await dialog.ShowAsync();
        }
        catch
        {
            // 弹窗失败时静默降级，不影响主流程
        }
    }

    // ---- 自动更新 ----

    /// <summary>检查并打开更新窗口。manual 为 true（手动点击）时无更新也显示历史版本；否则仅在新版本时提示。</summary>
    private async Task CheckForUpdatesAsync(bool manual = false)
    {
        if (_updateBusy)
        {
            return;
        }
        _updateBusy = true;
        var opened = false;
        try
        {
            var releases = await Task.Run(() => UpdateService.GetReleasesAsync(10));
            if (releases is not { Count: > 0 })
            {
                if (manual)
                {
                    await ShowMessageAsync("检查更新", "获取更新信息失败，请检查网络后重试。");
                }
                return;
            }

            var latest = releases[0];
            if (!manual && !UpdateService.IsNewer(latest))
            {
                // 启动静默检查且已是最新：不打扰用户
                return;
            }

            // 独立更新窗口：无灰色遮罩，可选定版本并展示标签与更新说明
            var win = new UpdateWindow(releases) { Topmost = Topmost };
            win.Closed += (_, _) => _updateBusy = false;
            win.Show(this);
            win.Activate();
            opened = true;
        }
        catch
        {
            if (manual)
            {
                await ShowMessageAsync("检查更新", "获取更新信息失败，请检查网络后重试。");
            }
        }
        finally
        {
            if (!opened)
            {
                _updateBusy = false;
            }
        }
    }

    // ---- 事件 ----

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_viewModel.Settings.LockWindow &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow is null || !_settingsWindow.IsVisible)
        {
            _settingsWindow = new SettingsWindow(_viewModel);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show(this);
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.Settings.CloseToExit)
        {
            Exit();
        }
        else
        {
            Hide();
        }
    }
}
