using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using MouseClicker.ViewModels;

namespace MouseClicker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private SettingsWindow? _settingsWindow;
    private TrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Icon = LoadWindowIcon();
        AppIcon.Source = LoadBitmap();
        CreateTrayIcon();

        _viewModel.WarningRequested += ShowWarning;
        Closed += (_, _) => _viewModel.Dispose();
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
        _viewModel.Dispose();
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

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Hide();
}
