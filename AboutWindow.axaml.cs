using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MouseClicker;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://MouseClicker/icon.jpg"));
            AboutIcon.Source = new Bitmap(stream);
        }
        catch
        {
            // 图标加载失败时忽略
        }
    }

    private const string DouyinUrl =
        "https://www.douyin.com/user/MS4wLjABAAAABDWaMze6oSVRdkv-3eq7K8B7iwh3ygR040JWsv9OJys";

    private void Douyin_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(DouyinUrl) { UseShellExecute = true });
        }
        catch
        {
            // 打开浏览器失败时忽略
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
