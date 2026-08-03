using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MouseClicker;

/// <summary>单实例提示窗口：第二个实例启动时以 FluentAvalonia 风格提示。</summary>
public partial class AlertWindow : Window
{
    public AlertWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e) => Close();
}
