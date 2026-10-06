using System.Windows.Input;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class BattingViewModel : ViewModelBase
{
	private readonly GameViewModel _gameViewModel;
	private readonly IHapticsService _hapticsService;
	private AtBat _currentAtBat;
	private bool _isTakeChoicesVisible;
	private bool _isSwingChoicesVisible;
	private bool _isHitChoicesVisible;
	private bool _isOutChoicesVisible;
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

	public bool IsTakeChoicesVisible
	{
		get => _isTakeChoicesVisible;
		private set => SetProperty(ref _isTakeChoicesVisible, value);
	}

	public bool IsSwingChoicesVisible
	{
		get => _isSwingChoicesVisible;
		private set => SetProperty(ref _isSwingChoicesVisible, value);
	}

	public bool IsHitChoicesVisible
	{
		get => _isHitChoicesVisible;
		private set => SetProperty(ref _isHitChoicesVisible, value);
	}

	public bool IsOutChoicesVisible
	{
		get => _isOutChoicesVisible;
		private set => SetProperty(ref _isOutChoicesVisible, value);
	}

	public ICommand TakeCommand { get; }
	public ICommand TakeBallCommand { get; }
	public ICommand TakeStrikeCommand { get; }
	public ICommand SwingCommand { get; }
	public ICommand SwingHitCommand { get; }
	public ICommand SwingMissCommand { get; }
	public ICommand SingleCommand { get; }
	public ICommand DoubleCommand { get; }
	public ICommand TripleCommand { get; }
	public ICommand FoulBallCommand { get; }
	public ICommand OutCommand { get; }
	public ICommand GroundOutCommand { get; }
	public ICommand FlyOutCommand { get; }
	public ICommand EndAtBatCommand { get; }

	public BattingViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		_gameViewModel = gameViewModel;
		_hapticsService = hapticsService;
		_currentAtBat = CreateOrResumeAtBat();

		TakeCommand = new RelayCommand(() => { _hapticsService.Tap(); SetChoiceMode(take: true); });
		TakeBallCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordTakeAsync(BattingEventType.TakeBall); });
		TakeStrikeCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordTakeAsync(BattingEventType.TakeStrike); });
		SwingCommand = new RelayCommand(() => { _hapticsService.Tap(); SetChoiceMode(take: false); });
		SwingHitCommand = new RelayCommand(() => { _hapticsService.Tap(); IsHitChoicesVisible = true; });
		SwingMissCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordSwingMissAsync(); });
		SingleCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordHitAsync(BattingEventType.SwingHitSingle, "Single"); });
		DoubleCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordHitAsync(BattingEventType.SwingHitDouble, "Double"); });
		TripleCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordHitAsync(BattingEventType.SwingHitTriple, "Triple"); });
		FoulBallCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordFoulBall(); });
		OutCommand = new RelayCommand(() =>
		{
			_hapticsService.Tap();
			IsHitChoicesVisible = false;
			IsOutChoicesVisible = true;
		});
		GroundOutCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordHitAsync(BattingEventType.SwingHitOutGround, "GroundOut"); });
		FlyOutCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await RecordHitAsync(BattingEventType.SwingHitOutFly, "FlyOut"); });
		EndAtBatCommand = new AsyncRelayCommand(async () => { _hapticsService.Tap(); await FinishAtBatAsync("Out"); });
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
			SetChoiceMode(take: false);
			return;
		}

		if (_atBatCompleted)
		{
			CurrentAtBat = CreateNewAtBat();
			Balls = 0;
			Strikes = 0;
			_atBatCompleted = false;
			_requestedAtBatNumber = CurrentAtBat.AtBatNumber;
			SetChoiceMode(take: false);
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
		SetChoiceMode(take: false);
	}

	public void Take() => SetChoiceMode(take: true);

	public void Swing() => SetChoiceMode(take: false);

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

	private void SetChoiceMode(bool take)
	{
		IsTakeChoicesVisible = take;
		IsSwingChoicesVisible = !take;
		IsHitChoicesVisible = false;
		IsOutChoicesVisible = false;
	}

	private async Task RecordTakeAsync(BattingEventType type)
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = type });
		if (type == BattingEventType.TakeBall)
		{
			Balls++;
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
			if (Strikes == 3)
			{
				await FinishStrikeoutAsync();
				return;
			}
		}

		SaveProgress();
		SetChoiceMode(take: false);
	}

	private async Task RecordSwingMissAsync()
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = BattingEventType.SwingMiss });
		Strikes++;
		if (Strikes == 3)
		{
			await FinishStrikeoutAsync();
			return;
		}

		SaveProgress();
		SetChoiceMode(take: false);
	}

	private Task RecordHitAsync(BattingEventType type, string result)
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = type });
		return FinishAtBatAsync(result);
	}

	private void RecordFoulBall()
	{
		CurrentAtBat.Events.Add(new BattingEvent { Type = BattingEventType.SwingHitOutFoul });
		if (Strikes < 2)
		{
			Strikes++;
		}

		SaveProgress();
		SetChoiceMode(take: false);
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
		IsTakeChoicesVisible = false;
		IsSwingChoicesVisible = false;
		IsHitChoicesVisible = false;
		IsOutChoicesVisible = false;
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
