using System.Runtime.InteropServices;

namespace MouseClicker.Services;

/// <summary>鼠标按键类型。</summary>
public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2,
}

/// <summary>点击方式。</summary>
public enum ClickMode
{
    /// <summary>单击：按下并立即释放。</summary>
    SingleClick = 0,

    /// <summary>双击：连续两次单击。</summary>
    DoubleClick = 1,

    /// <summary>按下并释放：同单击（按下→释放），单独保留以便扩展按住时长。</summary>
    PressRelease = 2,
}

/// <summary>
/// 基于 user32.dll 的全局鼠标操作服务。
/// 模拟点击使用 <c>SendInput</c>（相比 <c>mouse_event</c> 更稳定、不易被拦截）。
/// 坐标均为物理像素（与 GetCursorPos/SetCursorPos 保持一致，不做 DPI 换算）。
/// </summary>
public static class MouseService
{
    // ---- P/Invoke 结构 ----

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    // ---- 常量 ----

    private const uint INPUT_MOUSE = 0;

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    // ---- DllImport ----

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // ---- 对外 API ----

    /// <summary>获取当前鼠标位置（物理像素）。</summary>
    public static (int X, int Y) GetCursorPosition()
    {
        POINT p;
        GetCursorPos(out p);
        return (p.X, p.Y);
    }

    /// <summary>移动鼠标到指定坐标（物理像素）。</summary>
    public static bool MoveTo(int x, int y) => SetCursorPos(x, y);

    /// <summary>执行一次完整点击（按下→释放）。</summary>
    public static void Click(MouseButton button)
    {
        Down(button);
        Up(button);
    }

    /// <summary>执行双击。</summary>
    public static void DoubleClick(MouseButton button)
    {
        Click(button);
        Click(button);
    }

    /// <summary>按下指定按键。</summary>
    public static void Down(MouseButton button) => SendMouseEvent(GetButtonFlag(button, isDown: true));

    /// <summary>释放指定按键。</summary>
    public static void Up(MouseButton button) => SendMouseEvent(GetButtonFlag(button, isDown: false));

    // ---- 内部实现 ----

    private static void SendMouseEvent(uint dwFlags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new INPUTUNION
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    private static uint GetButtonFlag(MouseButton button, bool isDown) => button switch
    {
        MouseButton.Left => isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,
        MouseButton.Right => isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP,
        MouseButton.Middle => isDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP,
        _ => throw new ArgumentOutOfRangeException(nameof(button)),
    };
}
