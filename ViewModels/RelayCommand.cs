using System.Windows.Input;

namespace MouseClicker.ViewModels;

/// <summary>简化版 RelayCommand，实现 ICommand（Avalonia 无 CommandManager，事件手动触发）。</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Predicate<object?>? canExecute = null)
        : this(_ => execute(), canExecute)
    {
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged;

    /// <summary>CanExecute 变化时手动触发（本应用无动态 CanExecute，保留以备扩展）。</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
