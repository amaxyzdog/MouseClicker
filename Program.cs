using Avalonia;

namespace MouseClicker;

/// <summary>程序入口。</summary>
internal static class Program
{
    private const string MutexName = @"Local\MouseClicker.SingleInstance";

    /// <summary>第二个实例用于显示"已在运行"提示的启动参数。</summary>
    public const string AlertArg = "--alert-running";

    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // 单实例：已有实例运行时，以 FluentAvalonia 风格提示后退出
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(new[] { AlertArg });
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _singleInstanceMutex.ReleaseMutex();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect();
}
