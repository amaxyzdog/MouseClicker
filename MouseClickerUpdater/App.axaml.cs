using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace MouseClickerUpdater;

public class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Styles.Add(new FluentTheme());
            desktop.MainWindow = new MainWindow(desktop.Args ?? Array.Empty<string>());
        }
        base.OnFrameworkInitializationCompleted();
    }
}
