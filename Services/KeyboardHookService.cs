using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseClicker.Services;

/// <summary>
/// 低层全局键盘钩子（WH_KEYBOARD_LL）。
/// 无需管理员权限即可在后台全局响应按键，用于热键触发启动/停止。
/// </summary>
public sealed class KeyboardHookService : IDisposable
{
    /// <summary>任意按键按下（参数为虚拟键码 vkCode）。</summary>
    public event Action<int>? KeyDown;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private LowLevelKeyboardProc? _proc;
    private IntPtr _hookId = IntPtr.Zero;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>安装全局钩子（需在 UI 线程调用，回调经消息循环派发）。</summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    /// <summary>卸载全局钩子。</summary>
    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            KeyDown?.Invoke(vkCode);
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
