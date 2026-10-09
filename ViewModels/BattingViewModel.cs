using System.Windows.Input;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class BattingViewModel : ViewModelBase
{
	private readonly GameViewModel _gameViewModel;
	private readonly IHapticsService _hapticsService;
	private AtBat _currentAtBat;
	private bool _atBatCompleted;
	private int _balls;
	private int _strikes;
	private Guid _gameId;
	private int? _requestedAtBatNumber;

	public AtBat CurrentAtBat
	{
		get => _currentAtBat;
		private set => SetProperty(ref _currentAtBat, value);
	}

	public int Balls
	{
		get => _balls;
		private set => SetProperty(ref _balls, value);
	}

	public int Strikes
	{
		get => _strikes;
		private set => SetProperty(ref _strikes, value);
	}

	public ICommand TakeCommand { get; }
	public ICommand SwingCommand { get; }
	public ICommand EndAtBatCommand { get; }

	public BattingViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		_gameViewModel = gameViewModel;
		_hapticsService = hapticsService;
		_currentAtBat = CreateOrResumeAtBat();

		TakeCommand = new AsyncRelayCommand(SelectTakeResultAsync);
		SwingCommand = new AsyncRelayCommand(SelectSwingResultAsync);
		EndAtBatCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			_gameViewModel.AddGameLogEntry(
				"Batter recorded as out (at bat ended manually)",
				atBatNumber: CurrentAtBat.AtBatNumber);
			await FinishAtBatAsync("Out");
		});
		UpdateCountFromEvents();
	}

	public void StartAtBatIfNeeded()
	{
		var game = _gameViewModel.RequireCurrentGame();
		if (_gameId != game.Id)
		{
			_gameId = game.Id;
			CurrentAtBat = CreateOrResumeAtBat(_requestedAtBatNumber);
			_atBatCompleted = false;
			UpdateCountFromEvents();
			return;
		}

		if (_atBatCompleted)
		{
			CurrentAtBat = CreateNewAtBat();
			Balls = 0;
			Strikes = 0;
			_atBatCompleted = false;
			_requestedAtBatNumber = CurrentAtBat.AtBatNumber;
		}
	}

	public void LoadGame(Guid gameId, string context, int atBatNumber)
	{
		_gameViewModel.LoadGame(gameId, context);
		if (atBatNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(atBatNumber), "At-bat number must be positive.");
		}

		_requestedAtBatNumber = atBatNumber;
		CurrentAtBat = GetOrCreateAtBat(atBatNumber);
		_atBatCompleted = false;
		UpdateCountFromEvents();
	}

	public Task EndAtBat() => FinishAtBatAsync("Out");

	private AtBat CreateOrResumeAtBat(int? atBatNumber = null)
	{
		var game = _gameViewModel.RequireCurrentGame();
		_gameId = game.Id;
		if (atBatNumber is not null)
		{
			return GetOrCreateAtBat(atBatNumber.Value);
		}

		var activeAtBat = game.AtBats.LastOrDefault(atBat =>
			string.IsNullOrEmpty(atBat.Result) && !atBat.IsWalk && !atBat.IsStrikeout);
		if (activeAtBat is not null)
		{
			return activeAtBat;
		}

		return CreateNewAtBat();
	}

	private AtBat GetOrCreateAtBat(int atBatNumber)
	{
		var game = _gameViewModel.RequireCurrentGame();
		var atBat = game.AtBats.FirstOrDefault(item => item.AtBatNumber == atBatNumber);
		if (atBat is not null)
		{
			return atBat;
		}

		if (atBatNumber != game.AtBats.Count + 1)
		{
			throw new KeyNotFoundException($"At bat {atBatNumber} does not exist in the current game.");
		}

		var newAtBat = new AtBat { AtBatNumber = atBatNumber };
		game.AtBats.Add(newAtBat);
		_gameViewModel.SaveCurrentGame();
		return newAtBat;
	}

	private AtBat CreateNewAtBat()
	{
		var game = _gameViewModel.RequireCurrentGame();
		_gameId = game.Id;
		var atBat = new AtBat { AtBatNumber = game.AtBats.Count + 1 };
		game.AtBats.Add(atBat);
		_gameViewModel.SaveCurrentGame();
		return atBat;
	}

	private async Task SelectTakeResultAsync()
	{
		_hapticsService.Tap();
		var result = await Shell.Current.DisplayActionSheetAsync(
			"Take",
			"Cancel",
			null,
			"Ball",
			"Strike");

		switch (result)
		{
			case "Ball":
				_hapticsService.Tap();
				await RecordTakeAsync(BattingEventType.TakeBall);
				break;
			case "Strike":
				_hapticsService.Tap();
				await RecordTakeAsync(BattingEventType.TakeStrike);
				break;
		}
	}

	private async Task SelectSwingResultAsync()
	{
		_hapticsService.Tap();
		var result = await Shell.Current.DisplayActionSheetAsync(
			"Swing",
			"Cancel",
			null,
			"Hit",
			"Miss");

		switch (result)
		{
			case "Hit":
				_hapticsService.Tap();
				await SelectHitResultAsync();
				break;
			case "Miss":
				_hapticsService.Tap();
				await RecordSwingMissAsync();
				break;
		}
	}

	private async Task SelectHitResultAsync()
	{
		var result = await Shell.Current.DisplayActionSheetAsync(
			"Select hit result",
			"Cancel",
			null,
			"Single",
			"Double",
			"Triple",
			"Foulball",
			"Out");

		switch (result)
		{
			case "Single":
				_hapticsService.Tap();
				await RecordHitAsync(BattingEventType.SwingHitSingle, "Single");
				break;
			case "Double":
				_hapticsService.Tap();
				await RecordHitAsync(BattingEventType.SwingHitDouble, "Double");
				break;
			case "Triple":
				_hapticsService.Tap();
				await RecordHitAsync(BattingEventType.SwingHitTriple, "Triple");
				break;
			case "Foulball":
				_hapticsService.Tap();
				RecordFoulBall();
				break;
			case "Out":
				_hapticsService.Tap();
				await SelectOutTypeAsync();
				break;
		}
	}

	private async Task SelectOutTypeAsync()
	{
		var result = await Shell.Current.DisplayActionSheetAsync(
			"Select out type",
			"Cancel",
			null,
			"Ground Out",
			"Fly Out");

		switch (result)
		{
			case "Ground Out":
				_hapticsService.Tap();
				await RecordHitAsync(BattingEventType.SwingHitOutGround, "GroundOut");
				break;
			case "Fly Out":
				_hapticsService.Tap();
				await RecordHitAsync(BattingEventType.SwingHitOutFly, "FlyOut");
				break;
		}
	}

	private async Task RecordTakeAsync(BattingEventType type)
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = type });
		if (type == BattingEventType.TakeBall)
		{
			Balls++;
			LogBattingEvent($"Batter took ball {Balls}" + (Balls == 4 ? " (walk)" : string.Empty));
			if (Balls == 4)
			{
				CurrentAtBat.IsWalk = true;
				await FinishAtBatAsync("Walk");
				return;
			}
		}
		else
		{
			Strikes++;
			LogBattingEvent($"Batter took strike {Strikes}" + (Strikes == 3 ? " (strikeout)" : string.Empty));
			if (Strikes == 3)
			{
				await FinishStrikeoutAsync();
				return;
			}
		}

		SaveProgress();
	}

	private async Task RecordSwingMissAsync()
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = BattingEventType.SwingMiss });
		Strikes++;
		LogBattingEvent($"Batter swung and missed (strike {Strikes}" +
			(Strikes == 3 ? "; strikeout)" : ")"));
		if (Strikes == 3)
		{
			await FinishStrikeoutAsync();
			return;
		}

		SaveProgress();
	}

	private Task RecordHitAsync(BattingEventType type, string result)
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = type });
		var description = type switch
		{
			BattingEventType.SwingHitSingle => "Batter hit a single",
			BattingEventType.SwingHitDouble => "Batter hit a double",
			BattingEventType.SwingHitTriple => "Batter hit a triple",
			BattingEventType.SwingHitOutGround => "Batter hit a ground out",
			BattingEventType.SwingHitOutFly => "Batter hit a fly out",
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
		LogBattingEvent(description);
		return FinishAtBatAsync(result);
	}

	private void RecordFoulBall()
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = BattingEventType.SwingHitOutFoul });
		var countsAsStrike = Strikes < 2;
		if (countsAsStrike)
		{
			Strikes++;
		}

		LogBattingEvent(countsAsStrike
			? $"Batter hit a foul ball (strike {Strikes})"
			: $"Batter hit a foul ball (strike count remains {Strikes})");
		SaveProgress();
	}

	private Task FinishStrikeoutAsync()
	{
		CurrentAtBat.IsStrikeout = true;
		return FinishAtBatAsync("Strikeout");
	}

	private async Task FinishAtBatAsync(string result)
	{
		var game = _gameViewModel.RequireCurrentGame();
		CurrentAtBat.Result = result;
		_gameViewModel.SaveCurrentGame();
		_atBatCompleted = true;
		await Shell.Current.GoToAsync(
			AppShell.GameRoute,
			new Dictionary<string, object>
			{
				["gameId"] = game.Id,
				["context"] = "live",
				["atBatNumber"] = CurrentAtBat.AtBatNumber
			});
	}

	private void SaveProgress()
	{
		_gameViewModel.SaveCurrentGame();
	}

	private void LogBattingEvent(string description)
	{
		_gameViewModel.AddGameLogEntry(description, atBatNumber: CurrentAtBat.AtBatNumber);
	}

	private void UpdateCountFromEvents()
	{
		Balls = 0;
		Strikes = 0;
		foreach (var battingEvent in CurrentAtBat.Events)
		{
			switch (battingEvent.Type)
			{
				case BattingEventType.TakeBall:
					Balls++;
					break;
				case BattingEventType.TakeStrike:
				case BattingEventType.SwingMiss:
					Strikes++;
					break;
				case BattingEventType.SwingHitOutFoul when Strikes < 2:
					Strikes++;
					break;
			}
		}
	}
}
