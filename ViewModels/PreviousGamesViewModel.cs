using System.Collections.ObjectModel;
using System.Windows.Input;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class PreviousGamesViewModel : ViewModelBase
{
	private readonly IGameStore _gameStore;
	private readonly IHapticsService _hapticsService;
	private Game? _selectedGame;
	private string _context = string.Empty;

	public ObservableCollection<Game> Games { get; } = [];

	public Game? SelectedGame
	{
		get => _selectedGame;
		set => SetProperty(ref _selectedGame, value);
	}

	public ICommand SelectGameCommand { get; }

	public string Context
	{
		get => _context;
		private set => SetProperty(ref _context, value);
	}

	public PreviousGamesViewModel(IGameStore gameStore, IHapticsService hapticsService)
	{
		_gameStore = gameStore;
		_hapticsService = hapticsService;
		SelectGameCommand = new AsyncRelayCommand(SelectSelectedGameAsync);
		RefreshGames();
	}

	public void RefreshGames()
	{
		Games.Clear();
		foreach (var game in _gameStore.GetGames().Where(game => game.EndTime != default))
		{
			Games.Add(game);
		}

		if (SelectedGame is not null && Games.All(game => game.Id != SelectedGame.Id))
		{
			SelectedGame = null;
		}
	}

	public void LoadContext(string context)
	{
		Context = context;
	}

	private Task SelectSelectedGameAsync()
	{
		_hapticsService.Tap();
		if (SelectedGame is null)
		{
			return Task.CompletedTask;
		}

		return Shell.Current.GoToAsync(
			AppShell.GameReportRoute,
			new Dictionary<string, object>
			{
				["gameId"] = SelectedGame.Id,
				["context"] = "history"
			});
	}
}
