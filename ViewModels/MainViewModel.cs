using System.Windows.Input;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
	private readonly GameViewModel _gameViewModel;

	public ICommand NewGameCommand { get; }

	public ICommand GameReportCommand { get; }

	public bool IsGameReportAvailable => _gameViewModel.CurrentGame is not null;

	public string MainActionText =>
		_gameViewModel.CurrentGame is { EndTime: var endTime } && endTime == default
			? "Resume Game"
			: "New Game";

	public MainViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		_gameViewModel = gameViewModel;
		_gameViewModel.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(GameViewModel.CurrentGame))
			{
				OnPropertyChanged(nameof(MainActionText));
				OnPropertyChanged(nameof(IsGameReportAvailable));
			}
		};

		NewGameCommand = new AsyncRelayCommand(async () =>
		{
			hapticsService.Tap();
			if (gameViewModel.CurrentGame is { EndTime: var endTime } activeGame &&
				endTime == default)
			{
				await Shell.Current.GoToAsync(
					AppShell.GameRoute,
					new Dictionary<string, object>
					{
						["gameId"] = activeGame.Id,
						["context"] = "live"
					});
				return;
			}

			var opponentName = await Shell.Current.DisplayPromptAsync(
				"New Game",
				"Opponent name (optional)",
				"Start",
				"Cancel",
				placeholder: "Opponent name",
				maxLength: 60,
				keyboard: Keyboard.Text);
			if (opponentName is null)
			{
				return;
			}

			var game = gameViewModel.StartNewGame(opponentName.Trim());
			await Shell.Current.GoToAsync(
				AppShell.GameRoute,
				new Dictionary<string, object>
				{
					["gameId"] = game.Id,
					["context"] = "new"
				});
		});

		GameReportCommand = new AsyncRelayCommand(async () =>
		{
			hapticsService.Tap();
			var game = gameViewModel.CurrentGame
				?? throw new InvalidOperationException("No game is available to report.");
			await Shell.Current.GoToAsync(
				AppShell.GameReportRoute,
				new Dictionary<string, object>
				{
					["gameId"] = game.Id,
					["context"] = game.EndTime == default ? "live" : "ended"
				});
		});
	}
}
