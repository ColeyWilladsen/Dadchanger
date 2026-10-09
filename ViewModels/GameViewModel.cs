using System.Windows.Input;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class GameViewModel : ViewModelBase
{
	private readonly IGameStore _gameStore;
	private readonly IHapticsService _hapticsService;
	private Game? _currentGame;
	private string _context = string.Empty;
	private int? _upcomingInningNumber;

	public Game? CurrentGame
	{
		get => _currentGame;
		private set
		{
			if (SetProperty(ref _currentGame, value))
			{
				OnPropertyChanged(nameof(OpponentDisplayName));
				OnPropertyChanged(nameof(WalkCount));
				OnPropertyChanged(nameof(StrikeoutCount));
			}
		}
	}

	public string OpponentDisplayName => string.IsNullOrWhiteSpace(CurrentGame?.OpponentName)
		? "Opponent: Not set"
		: $"Opponent: {CurrentGame.OpponentName}";

	public int WalkCount => CurrentGame?.CountWalks() ?? 0;

	public int StrikeoutCount => CurrentGame?.CountStrikeouts() ?? 0;

	public string Context
	{
		get => _context;
		private set => SetProperty(ref _context, value);
	}

	public ICommand GoToPitchingCommand { get; }

	public ICommand GoToBattingCommand { get; }

	public ICommand PitchingCommand => GoToPitchingCommand;

	public ICommand BattingCommand => GoToBattingCommand;

	public ICommand EndGameCommand { get; }

	public int CurrentInningNumber
	{
		get
		{
			var game = RequireCurrentGame();
			var latestInningNumber = game.PitchingInnings.LastOrDefault()?.InningNumber ?? 1;
			return Math.Max(latestInningNumber, Math.Max(game.ActiveInningNumber, _upcomingInningNumber ?? 1));
		}
	}

	public int CurrentAtBatNumber
	{
		get
		{
			var game = RequireCurrentGame();
			var activeAtBat = game.AtBats.LastOrDefault(atBat =>
				string.IsNullOrEmpty(atBat.Result) && !atBat.IsWalk && !atBat.IsStrikeout);
			return activeAtBat?.AtBatNumber ?? game.AtBats.Count + 1;
		}
	}

	public GameViewModel(IGameStore gameStore, IHapticsService hapticsService)
	{
		_gameStore = gameStore;
		_hapticsService = hapticsService;
		CurrentGame = _gameStore.GetGames().LastOrDefault(game => game.EndTime == default);
		if (CurrentGame is not null)
		{
			Context = "live";
			_upcomingInningNumber = CurrentGame.ActiveInningNumber;
		}

		GoToPitchingCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			await NavigateToGamePageAsync(
				AppShell.PitchingRoute,
				"live",
				("inningNumber", CurrentInningNumber));
		});
		GoToBattingCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			await NavigateToGamePageAsync(
				AppShell.BattingRoute,
				"live",
				("atBatNumber", CurrentAtBatNumber));
		});
		EndGameCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			await EndGameAsync();
		});
	}

	public Game StartNewGame(string? opponentName = null)
	{
		var game = _gameStore.StartNewGame(opponentName);
		_upcomingInningNumber = null;
		CurrentGame = game;
		Context = "new";
		return game;
	}

	public void LoadGame(Guid gameId, string context)
	{
		if (CurrentGame?.Id != gameId)
		{
			_upcomingInningNumber = null;
		}

		CurrentGame = _gameStore.GetGameById(gameId)
			?? throw new KeyNotFoundException($"No game with id '{gameId}' exists in the game store.");
		Context = context;
	}

	public void SetUpcomingInningNumber(int inningNumber)
	{
		if (inningNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(inningNumber), "Inning number must be positive.");
		}

		_upcomingInningNumber = inningNumber;
		var game = RequireCurrentGame();
		game.ActiveInningNumber = inningNumber;
		SaveCurrentGame();
	}

	public Game RequireCurrentGame()
	{
		return CurrentGame ?? throw new InvalidOperationException("No game is currently in progress.");
	}

	public void SaveCurrentGame()
	{
		if (CurrentGame is { } game)
		{
			_gameStore.UpdateGame(game);
			OnPropertyChanged(nameof(WalkCount));
			OnPropertyChanged(nameof(StrikeoutCount));
		}
	}

	public void AddGameLogEntry(
		string description,
		int? inningNumber = null,
		int? atBatNumber = null)
	{
		var game = RequireCurrentGame();
		game.GameLogEntries.Add(new GameLogEntry
		{
			Timestamp = DateTime.Now,
			InningNumber = inningNumber,
			AtBatNumber = atBatNumber,
			Description = description
		});
		SaveCurrentGame();
	}

	private async Task EndGameAsync()
	{
		var game = RequireCurrentGame();
		_gameStore.EndGame(game);
		OnPropertyChanged(nameof(CurrentGame));
		await Shell.Current.GoToAsync(
			AppShell.GameReportRoute,
			new Dictionary<string, object>
			{
				["gameId"] = game.Id,
				["context"] = "ended"
			});
	}

	private Task NavigateToGamePageAsync(string route, string context, (string Key, object Value) extraParameter)
	{
		var game = RequireCurrentGame();
		var query = new Dictionary<string, object>
		{
			["gameId"] = game.Id,
			["context"] = context
		};
		query[extraParameter.Key] = extraParameter.Value;

		return Shell.Current.GoToAsync(
			route,
			query);
	}
}
