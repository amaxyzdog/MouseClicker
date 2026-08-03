using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MouseClicker.Services;
using MouseClicker.ViewModels;

namespace MouseClicker;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _positionTimer;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // 主窗口置顶时，设置窗口同样置顶，避免被主窗口遮挡
        Topmost = _viewModel.Settings.IsTopmost;

        // 实时预览鼠标坐标（200ms 刷新，跟随鼠标移动）
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _positionTimer.Tick += (_, _) =>
        {
            var (x, y) = MouseService.GetCursorPosition();
            LivePositionText.Text = $"{x}, {y}";
        };
        _positionTimer.Start();
        Closed += (_, _) => _positionTimer.Stop();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void ResetButton_Click(object? sender, RoutedEventArgs e) => _viewModel.Settings.Reset();

    private void DoneButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void AboutButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = new AboutWindow();
        window.Show(this);
        window.Activate();
    }

    private void HotKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        var settings = _viewModel.Settings;
        settings.IsCapturingHotKey = !settings.IsCapturingHotKey;
        if (settings.IsCapturingHotKey)
        {
            // 让按钮失去焦点，避免回车/空格误触发按钮再切回
            FocusManager?.ClearFocus();
        }
    }

    /// <summary>坐标锁定模式下，按 Ctrl+S 以当前鼠标位置作为目标坐标。</summary>
    private void Root_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S
            && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            && !_viewModel.Settings.IsCapturingHotKey
            && _viewModel.Settings.LockPosition
            && e.Source is not TextBox)
        {
            _viewModel.PickPositionCommand.Execute(null);
            e.Handled = true;
        }
    }
}
