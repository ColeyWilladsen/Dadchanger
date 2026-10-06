using System.Windows.Input;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
	public ICommand NewGameCommand { get; }

	public ICommand PreviousGamesCommand { get; }

	public MainViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		NewGameCommand = new AsyncRelayCommand(async () =>
		{
			hapticsService.Tap();
			var game = gameViewModel.StartNewGame();
			await Shell.Current.GoToAsync(
				AppShell.GameRoute,
				new Dictionary<string, object>
				{
					["gameId"] = game.Id,
					["context"] = "new"
				});
		});
		PreviousGamesCommand = new AsyncRelayCommand(async () =>
		{
			hapticsService.Tap();
			await Shell.Current.GoToAsync(
				AppShell.PreviousGamesRoute,
				new Dictionary<string, object> { ["context"] = "history" });
		});
	}
}
