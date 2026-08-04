using System.Text.Json;
using Avalonia.Threading;
using MouseClicker.Services;

namespace MouseClicker.ViewModels;

/// <summary>下拉选项：值 + 显示文本（Avalonia ComboBox 无 DisplayMemberPath，用 ItemTemplate 绑定 Label）。</summary>
public sealed class SelectableOption<T>
{
    public SelectableOption(T value, string label)
    {
        Value = value;
        Label = label;
    }

    public T Value { get; }

    public string Label { get; }
}

/// <summary>
/// 连点参数与界面设置（设置窗口绑定，主窗口通过 <see cref="MainViewModel.Settings"/> 引用）。
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    /// <summary>默认热键：F2。</summary>
    public const int DefaultHotKey = 0x72;

    /// <summary>配置文件路径（%AppData%\MouseClicker\config.json）。</summary>
    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseClicker", "config.json");

    /// <summary>配置保存防抖定时器：避免拖动滑块/切换开关时每个属性变化都立即写盘。</summary>
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool _savePending;

    public SettingsViewModel()
    {
        Load();
        // 任意设置变化自动保存（500ms 防抖：连续变化只在停止后写一次盘）
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            if (_savePending)
            {
                _savePending = false;
                Save();
            }
        };
        PropertyChanged += (_, _) =>
        {
            _savePending = true;
            _saveTimer.Stop();
            _saveTimer.Start();
        };
    }

    /// <summary>程序退出前调用，将尚未落盘的更改立即写入配置文件。</summary>
    public void FlushSave()
    {
        _saveTimer.Stop();
        if (_savePending)
        {
            _savePending = false;
            Save();
        }
    }

    private int _interval = 100;
    private bool _useRandomDelay;
    private int _randomDelayMax = 200;
    private MouseButton _clickType = MouseButton.Left;
    private ClickMode _clickMode = ClickMode.SingleClick;
    private bool _lockPosition;
    private bool _isTopmost = true;
    private bool _lockWindow;
    private int? _targetX;
    private int? _targetY;
    private int _hotKey = DefaultHotKey;
    private bool _isCapturingHotKey;
    private bool _autoCheckUpdate = true;
    private bool _closeToExit;

    // ---- ComboBox 选项 ----

    public IReadOnlyList<SelectableOption<MouseButton>> ClickTypes { get; } = new[]
    {
        new SelectableOption<MouseButton>(MouseButton.Left, "左键"),
        new SelectableOption<MouseButton>(MouseButton.Right, "右键"),
        new SelectableOption<MouseButton>(MouseButton.Middle, "中键"),
    };

    public IReadOnlyList<SelectableOption<ClickMode>> ClickModes { get; } = new[]
    {
        new SelectableOption<ClickMode>(ClickMode.SingleClick, "单击"),
        new SelectableOption<ClickMode>(ClickMode.DoubleClick, "双击"),
        new SelectableOption<ClickMode>(ClickMode.PressRelease, "按下并释放"),
    };

    // ---- 点击参数 ----

    /// <summary>点击间隔（毫秒，1~1000）。</summary>
    public int Interval
    {
        get => _interval;
        set
        {
            value = Math.Clamp(value, 1, 1000);
            SetProperty(ref _interval, value);
        }
    }

    /// <summary>是否启用随机延迟。</summary>
    public bool UseRandomDelay
    {
        get => _useRandomDelay;
        set => SetProperty(ref _useRandomDelay, value);
    }

    /// <summary>随机延迟上限（毫秒，在 Interval 基础上叠加 0~上限）。</summary>
    public int RandomDelayMax
    {
        get => _randomDelayMax;
        set
        {
            value = Math.Clamp(value, 0, 1000);
            SetProperty(ref _randomDelayMax, value);
        }
    }

    public MouseButton ClickType
    {
        get => _clickType;
        set
        {
            if (SetProperty(ref _clickType, value))
            {
                OnPropertyChanged(nameof(SelectedClickType));
            }
        }
    }

    /// <summary>当前选中的按键下拉项（ComboBox SelectedItem 双向绑定）。</summary>
    public SelectableOption<MouseButton>? SelectedClickType
    {
        get => ClickTypes.FirstOrDefault(o => o.Value == ClickType);
        set
        {
            if (value is not null)
            {
                ClickType = value.Value;
            }
        }
    }

    public ClickMode ClickMode
    {
        get => _clickMode;
        set
        {
            if (SetProperty(ref _clickMode, value))
            {
                OnPropertyChanged(nameof(SelectedClickMode));
            }
        }
    }

    /// <summary>当前选中的点击方式下拉项（ComboBox SelectedItem 双向绑定）。</summary>
    public SelectableOption<ClickMode>? SelectedClickMode
    {
        get => ClickModes.FirstOrDefault(o => o.Value == ClickMode);
        set
        {
            if (value is not null)
            {
                ClickMode = value.Value;
            }
        }
    }

    // ---- 坐标与窗口 ----

    /// <summary>是否锁定目标坐标（每次点击前移动鼠标）。</summary>
    public bool LockPosition
    {
        get => _lockPosition;
        set => SetProperty(ref _lockPosition, value);
    }

    /// <summary>窗口是否置顶。</summary>
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    /// <summary>锁定主窗口位置（禁止拖动）。</summary>
    public bool LockWindow
    {
        get => _lockWindow;
        set => SetProperty(ref _lockWindow, value);
    }

    // ---- 更新 ----

    /// <summary>启动时是否自动检查更新。</summary>
    public bool AutoCheckUpdate
    {
        get => _autoCheckUpdate;
        set => SetProperty(ref _autoCheckUpdate, value);
    }

    /// <summary>点击关闭按钮后是否直接退出程序（否：最小化到系统托盘）。</summary>
    public bool CloseToExit
    {
        get => _closeToExit;
        set => SetProperty(ref _closeToExit, value);
    }

    public int? TargetX
    {
        get => _targetX;
        set
        {
            if (SetProperty(ref _targetX, value))
            {
                OnPropertyChanged(nameof(TargetText));
            }
        }
    }

    public int? TargetY
    {
        get => _targetY;
        set
        {
            if (SetProperty(ref _targetY, value))
            {
                OnPropertyChanged(nameof(TargetText));
            }
        }
    }

    public string TargetText => TargetX is { } x && TargetY is { } y ? $"{x}, {y}" : "未设置";

    // ---- 热键 ----

    /// <summary>后台启停热键虚拟键码。</summary>
    public int HotKey
    {
        get => _hotKey;
        set
        {
            if (SetProperty(ref _hotKey, value))
            {
                OnPropertyChanged(nameof(HotKeyText));
                OnPropertyChanged(nameof(HotKeyButtonText));
                OnPropertyChanged(nameof(HotKeyHint));
            }
        }
    }

    /// <summary>是否处于热键录制状态（全局钩子捕获下一个按键）。</summary>
    public bool IsCapturingHotKey
    {
        get => _isCapturingHotKey;
        set
        {
            if (SetProperty(ref _isCapturingHotKey, value))
            {
                OnPropertyChanged(nameof(HotKeyButtonText));
                OnPropertyChanged(nameof(HotKeyHint));
            }
        }
    }

    public string HotKeyText => GetHotKeyDisplayName(HotKey);

    public string HotKeyButtonText => IsCapturingHotKey ? "请按下新热键…（Esc 取消）" : HotKeyText;

    public string HotKeyHint => IsCapturingHotKey
        ? "正在录制，请按下新热键…"
        : $"{HotKeyText} 后台启动/停止（可在设置中修改）";

    /// <summary>恢复默认设置。</summary>
    public void Reset()
    {
        Interval = 100;
        UseRandomDelay = false;
        RandomDelayMax = 200;
        ClickType = MouseButton.Left;
        ClickMode = ClickMode.SingleClick;
        LockPosition = false;
        IsTopmost = true;
        LockWindow = false;
        TargetX = null;
        TargetY = null;
        IsCapturingHotKey = false;
        HotKey = DefaultHotKey;
        AutoCheckUpdate = true;
        CloseToExit = false;
    }

    /// <summary>
    /// 虚拟键码 → 可读按键名。
    /// 注意：Avalonia 的 Key 枚举值 ≠ Win32 虚拟键码（如 F1=90 而 VK_F1=0x70），
    /// 因此必须直接按 Win32 虚拟键码映射，不能强转 Key 枚举。
    /// </summary>
    public static string GetHotKeyDisplayName(int vkCode)
    {
        if (vkCode >= 0x30 && vkCode <= 0x39)
        {
            return ((char)('0' + vkCode - 0x30)).ToString();
        }

        if (vkCode >= 0x41 && vkCode <= 0x5A)
        {
            return ((char)('A' + vkCode - 0x41)).ToString();
        }

        if (vkCode >= 0x60 && vkCode <= 0x69)
        {
            return $"小键盘{vkCode - 0x60}";
        }

        if (vkCode >= 0x70 && vkCode <= 0x87)
        {
            return $"F{vkCode - 0x70 + 1}";
        }

        return vkCode switch
        {
            0x08 => "退格",
            0x09 => "Tab",
            0x0D => "回车",
            0x1B => "Esc",
            0x20 => "空格",
            0x21 => "PgUp",
            0x22 => "PgDn",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "←", 0x26 => "↑", 0x27 => "→", 0x28 => "↓",
            0x2C => "PrintScreen",
            0x2D => "Insert",
            0x2E => "Del",
            0x90 => "NumLock",
            0x91 => "ScrollLock",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-",
            0xBE => ".", 0xBF => "/", 0xC0 => "`",
            0xDB => "[", 0xDC => "\\", 0xDD => "]", 0xDE => "'",
            _ => $"键{vkCode}",
        };
    }

    // ---- 配置持久化 ----

    /// <summary>序列化用的配置数据。</summary>
    private sealed class SettingsData
    {
        public int Interval { get; set; } = 100;
        public bool UseRandomDelay { get; set; }
        public int RandomDelayMax { get; set; } = 200;
        public MouseButton ClickType { get; set; } = MouseButton.Left;
        public ClickMode ClickMode { get; set; } = ClickMode.SingleClick;
        public bool LockPosition { get; set; }
        public bool IsTopmost { get; set; } = true;
        public bool LockWindow { get; set; }
        public int? TargetX { get; set; }
        public int? TargetY { get; set; }
        public int HotKey { get; set; } = DefaultHotKey;
        public bool AutoCheckUpdate { get; set; } = true;
        public bool CloseToExit { get; set; }
    }

    /// <summary>从配置文件加载设置（文件不存在或损坏时使用默认值）。</summary>
    private void Load()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return;
            }

            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_configPath));
            if (data is null)
            {
                return;
            }

            Interval = data.Interval;
            UseRandomDelay = data.UseRandomDelay;
            RandomDelayMax = data.RandomDelayMax;
            ClickType = data.ClickType;
            ClickMode = data.ClickMode;
            LockPosition = data.LockPosition;
            IsTopmost = data.IsTopmost;
            LockWindow = data.LockWindow;
            TargetX = data.TargetX;
            TargetY = data.TargetY;
            HotKey = data.HotKey;
            AutoCheckUpdate = data.AutoCheckUpdate;
            CloseToExit = data.CloseToExit;
        }
        catch
        {
            // 配置损坏时保持默认值
        }
    }

    /// <summary>将当前设置写入配置文件。</summary>
    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            var data = new SettingsData
            {
                Interval = Interval,
                UseRandomDelay = UseRandomDelay,
                RandomDelayMax = RandomDelayMax,
                ClickType = ClickType,
                ClickMode = ClickMode,
                LockPosition = LockPosition,
                IsTopmost = IsTopmost,
                LockWindow = LockWindow,
                TargetX = TargetX,
                TargetY = TargetY,
                HotKey = HotKey,
                AutoCheckUpdate = AutoCheckUpdate,
                CloseToExit = CloseToExit,
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 写文件失败（如无权限）时忽略，不影响使用
        }
    }
}
