using System.Windows.Input;

namespace Dadchanger.ViewModels;

public sealed class RelayCommand : ICommand
{
	private readonly Action<object?> _execute;
	private readonly Predicate<object?>? _canExecute;

	public RelayCommand(Action execute)
		: this(_ => execute())
	{
	}

	public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
	{
		_execute = execute;
		_canExecute = canExecute;
	}

	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

	public void Execute(object? parameter) => _execute(parameter);

	public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand : ICommand
{
	private readonly Func<Task> _execute;
	private bool _isExecuting;

	public AsyncRelayCommand(Func<Task> execute)
	{
		_execute = execute;
	}

	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => !_isExecuting;

	public async void Execute(object? parameter)
	{
		if (_isExecuting)
		{
			return;
		}

		_isExecuting = true;
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		try
		{
			await _execute();
		}
		finally
		{
			_isExecuting = false;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
