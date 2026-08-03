using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using MouseClicker.Services;

namespace MouseClicker.ViewModels;

/// <summary>
/// 主界面 ViewModel。
/// 点击调度使用 DispatcherTimer（UI 线程），天然线程安全；
/// 全局热键经键盘钩子回调（同样在 UI 线程消息循环中派发）。
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly Brush RunningBrush = new SolidColorBrush(Color.FromRgb(229, 72, 77));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(79, 110, 247));

    private readonly DispatcherTimer _clickTimer;
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly Random _random = new();

    private bool _isRunning;
    private DateTime _lastToggleAt = DateTime.MinValue;

    public MainViewModel()
    {
        ToggleCommand = new RelayCommand(_ => ToggleRunning());
        PickPositionCommand = new RelayCommand(_ =>
        {
            var (x, y) = MouseService.GetCursorPosition();
            Settings.TargetX = x;
            Settings.TargetY = y;
        });

        _clickTimer = new DispatcherTimer();
        _clickTimer.Tick += (_, _) => ClickTick();

        // 设置项变化时刷新主窗口显示
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SettingsViewModel.Interval)
                or nameof(SettingsViewModel.UseRandomDelay)
                or nameof(SettingsViewModel.RandomDelayMax))
            {
                OnPropertyChanged(nameof(IntervalDisplay));
            }

            if (e.PropertyName is nameof(SettingsViewModel.ClickMode)
                or nameof(SettingsViewModel.ClickType))
            {
                OnPropertyChanged(nameof(ClickModeDisplay));
            }
        };

        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.Install();
    }

    /// <summary>连点与界面设置。</summary>
    public SettingsViewModel Settings { get; } = new();

    // ---- 命令 ----

    public RelayCommand ToggleCommand { get; }
    public RelayCommand PickPositionCommand { get; }

    /// <summary>需要弹窗提示时的回调（由 UI 层实现，替换 WPF 的 MessageBox）。</summary>
    public event Action<string>? WarningRequested;

    // ---- 运行状态 ----

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(StartStopSymbol));
                OnPropertyChanged(nameof(StartStopToolTip));
                OnPropertyChanged(nameof(StartStopBrush));
            }
        }
    }

    /// <summary>启动/停止按钮图标（FluentAvalonia Symbol）。</summary>
    public Symbol StartStopSymbol => IsRunning ? Symbol.Pause : Symbol.Play;

    public string StartStopToolTip => IsRunning ? "停止连点" : "开始连点";

    public Brush StartStopBrush => IsRunning ? RunningBrush : AccentBrush;

    /// <summary>主窗口状态行显示的点击模式文本（按键 + 点击方式）。</summary>
    public string ClickModeDisplay => $"{GetClickTypeText()} {GetClickModeText()}";

    private string GetClickTypeText() => Settings.ClickType switch
    {
        MouseButton.Right => "右键",
        MouseButton.Middle => "中键",
        _ => "左键",
    };

    private string GetClickModeText() => Settings.ClickMode switch
    {
        ClickMode.DoubleClick => "双击",
        ClickMode.PressRelease => "按下并释放",
        _ => "单击",
    };

    /// <summary>主窗口状态行显示的间隔文本（含随机延迟标记）。</summary>
    public string IntervalDisplay => Settings.UseRandomDelay
        ? $"{Settings.Interval}ms + 随机{Settings.RandomDelayMax}ms"
        : $"{Settings.Interval}ms";

    // ---- 命令实现 ----

    private void ToggleRunning()
    {
        if (IsRunning)
        {
            Stop();
        }
        else
        {
            Start();
        }
    }

    private void Start()
    {
        if (Settings.LockPosition && (Settings.TargetX is null || Settings.TargetY is null))
        {
            WarningRequested?.Invoke("已开启坐标锁定，请先在设置中获取目标坐标。");
            return;
        }

        _clickTimer.Interval = TimeSpan.FromMilliseconds(GetEffectiveInterval());
        _clickTimer.Start();
        IsRunning = true;
    }

    private void Stop()
    {
        _clickTimer.Stop();
        IsRunning = false;
    }

    /// <summary>计算本次实际间隔（基础间隔 + 随机延迟）。</summary>
    private int GetEffectiveInterval()
    {
        int interval = Settings.Interval;
        if (Settings.UseRandomDelay && Settings.RandomDelayMax > 0)
        {
            interval += _random.Next(0, Settings.RandomDelayMax + 1);
        }
        return interval;
    }

    private void ClickTick()
    {
        try
        {
            if (Settings.LockPosition && Settings.TargetX is { } x && Settings.TargetY is { } y)
            {
                MouseService.MoveTo(x, y);
            }

            switch (Settings.ClickMode)
            {
                case ClickMode.SingleClick:
                    MouseService.Click(Settings.ClickType);
                    break;
                case ClickMode.DoubleClick:
                    MouseService.DoubleClick(Settings.ClickType);
                    break;
                case ClickMode.PressRelease:
                    MouseService.Down(Settings.ClickType);
                    MouseService.Up(Settings.ClickType);
                    break;
            }
        }
        catch (Exception)
        {
            // 捕获异常（如坐标越界），停止连点并重置 UI，避免崩溃
            Stop();
        }
        finally
        {
            // 随机延迟：每次点击后重新计算下一次间隔
            _clickTimer.Interval = TimeSpan.FromMilliseconds(GetEffectiveInterval());
        }
    }

    // ---- 全局热键 ----

    private void OnGlobalKeyDown(int vkCode)
    {
        // 热键录制状态：捕获下一个按键作为新热键
        if (Settings.IsCapturingHotKey)
        {
            if (vkCode == 0x1B) // Esc 取消录制
            {
                Settings.IsCapturingHotKey = false;
                return;
            }

            // 忽略纯修饰键：Shift(0x10) Ctrl(0x11) Alt(0x12) Win(0x5B/0x5C) CapsLock(0x14)
            if (vkCode is (>= 0x10 and <= 0x12) or 0x5B or 0x5C or 0x14)
            {
                return;
            }

            Settings.IsCapturingHotKey = false;
            Settings.HotKey = vkCode;
            return;
        }

        if (vkCode == Settings.HotKey)
        {
            // 按住热键时系统会连续触发 KeyDown，150ms 内忽略重复触发，避免反复启停
            if ((DateTime.Now - _lastToggleAt).TotalMilliseconds < 150)
            {
                return;
            }
            _lastToggleAt = DateTime.Now;
            ToggleRunning();
        }
    }

    // ---- 资源释放 ----

    public void Dispose()
    {
        _clickTimer.Stop();
        _keyboardHook.Dispose();
    }
}
