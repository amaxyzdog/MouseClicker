using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MouseClicker;

/// <summary>应用入口。</summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 第二个实例：仅显示"已在运行"提示；正常实例：显示主窗口
            desktop.MainWindow = desktop.Args?.Contains(Program.AlertArg) == true
                ? new AlertWindow()
                : new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
