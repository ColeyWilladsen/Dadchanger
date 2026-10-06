using System.Windows.Input;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class PitchingViewModel : ViewModelBase
{
	private readonly GameViewModel _gameViewModel;
	private readonly IHapticsService _hapticsService;
	private PitchingInning _currentInning;
	private Guid _gameId;
	private int? _requestedInningNumber;
	private PitchAction? _selectedAction;
	private PitchType? _selectedPitchType;
	private bool _showPitchTypeOptions;
	private bool _showHitResultOptions;
	private bool _showOutTypeOptions;

	public PitchingInning CurrentInning
	{
		get => _currentInning;
		private set => SetProperty(ref _currentInning, value);
	}

	public bool ShowPitchTypeOptions
	{
		get => _showPitchTypeOptions;
		private set => SetProperty(ref _showPitchTypeOptions, value);
	}

	public bool ShowHitResultOptions
	{
		get => _showHitResultOptions;
		private set => SetProperty(ref _showHitResultOptions, value);
	}

	public bool ShowOutTypeOptions
	{
		get => _showOutTypeOptions;
		private set => SetProperty(ref _showOutTypeOptions, value);
	}

	public ICommand StrikeCommand { get; }
	public ICommand BallCommand { get; }
	public ICommand HitCommand { get; }
	public ICommand FastballCommand { get; }
	public ICommand CurveballCommand { get; }
	public ICommand ChangeupCommand { get; }
	public ICommand SingleCommand { get; }
	public ICommand DoubleCommand { get; }
	public ICommand TripleCommand { get; }
	public ICommand FoulballCommand { get; }
	public ICommand OutCommand { get; }
	public ICommand GroundOutCommand { get; }
	public ICommand FlyOutCommand { get; }
	public ICommand EndInningCommand { get; }

	public PitchingViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		_gameViewModel = gameViewModel;
		_hapticsService = hapticsService;
		_currentInning = GetOrCreateCurrentInning();

		StrikeCommand = new RelayCommand(() => { _hapticsService.Tap(); BeginAction(PitchAction.Strike); });
		BallCommand = new RelayCommand(() => { _hapticsService.Tap(); BeginAction(PitchAction.Ball); });
		HitCommand = new RelayCommand(() => { _hapticsService.Tap(); BeginAction(PitchAction.Hit); });
		FastballCommand = new RelayCommand(() => { _hapticsService.Tap(); SelectPitchType(PitchType.Fastball); });
		CurveballCommand = new RelayCommand(() => { _hapticsService.Tap(); SelectPitchType(PitchType.Curveball); });
		ChangeupCommand = new RelayCommand(() => { _hapticsService.Tap(); SelectPitchType(PitchType.Changeup); });
		SingleCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitSingle); });
		DoubleCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitDouble); });
		TripleCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitTriple); });
		FoulballCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitFoulBall); });
		OutCommand = new RelayCommand(() =>
		{
			_hapticsService.Tap();
			ShowHitResultOptions = false;
			ShowOutTypeOptions = true;
		});
		GroundOutCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitOutGround); });
		FlyOutCommand = new RelayCommand(() => { _hapticsService.Tap(); RecordHitResult(PitchingEventType.HitOutFly); });
		EndInningCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			await EndInningAsync();
		});
	}

	public void BeginAction(PitchAction action)
	{
		_selectedAction = action;
		_selectedPitchType = null;
		ShowPitchTypeOptions = true;
		ShowHitResultOptions = false;
		ShowOutTypeOptions = false;
	}

	public void SelectPitchType(PitchType pitchType)
	{
		if (_selectedAction is null)
		{
			throw new InvalidOperationException("Select Strike, Ball, or Hit before choosing a pitch type.");
		}

		_selectedPitchType = pitchType;
		switch (_selectedAction)
		{
			case PitchAction.Strike:
				RecordPitch(pitchType switch
				{
					PitchType.Fastball => PitchingEventType.StrikeFastball,
					PitchType.Curveball => PitchingEventType.StrikeCurveball,
					PitchType.Changeup => PitchingEventType.StrikeChangeup,
					_ => throw new ArgumentOutOfRangeException(nameof(pitchType))
				}, pitchType);
				HideChoices();
				break;
			case PitchAction.Ball:
				RecordPitch(pitchType switch
				{
					PitchType.Fastball => PitchingEventType.BallFastball,
					PitchType.Curveball => PitchingEventType.BallCurveball,
					PitchType.Changeup => PitchingEventType.BallChangeup,
					_ => throw new ArgumentOutOfRangeException(nameof(pitchType))
				}, pitchType);
				HideChoices();
				break;
			case PitchAction.Hit:
				ShowPitchTypeOptions = false;
				ShowHitResultOptions = true;
				ShowOutTypeOptions = false;
				break;
		}
	}

	public void RecordStrike(PitchingEventType type) =>
		RecordPitch(type, null,
			PitchingEventType.StrikeFastball,
			PitchingEventType.StrikeCurveball,
			PitchingEventType.StrikeChangeup);

	public void RecordBall(PitchingEventType type) =>
		RecordPitch(type, null,
			PitchingEventType.BallFastball,
			PitchingEventType.BallCurveball,
			PitchingEventType.BallChangeup);

	public void RecordHit(PitchingEventType type) =>
		RecordPitch(type, _selectedPitchType,
			PitchingEventType.HitSingle,
			PitchingEventType.HitDouble,
			PitchingEventType.HitTriple,
			PitchingEventType.HitFoulBall,
			PitchingEventType.HitOutGround,
			PitchingEventType.HitOutFly);

	public async Task EndInningAsync()
	{
		var nextInning = new PitchingInning { InningNumber = CurrentInning.InningNumber + 1 };
		var game = _gameViewModel.RequireCurrentGame();
		game.PitchingInnings.Add(nextInning);
		CurrentInning = nextInning;
		_requestedInningNumber = nextInning.InningNumber;
		_gameViewModel.SaveCurrentGame();
		await Shell.Current.GoToAsync(
			AppShell.GameRoute,
			new Dictionary<string, object>
			{
				["gameId"] = game.Id,
				["context"] = "live",
				["inningNumber"] = nextInning.InningNumber
			});
	}

	public void RefreshCurrentInning()
	{
		var game = _gameViewModel.RequireCurrentGame();
		_gameId = game.Id;
		CurrentInning = GetOrCreateCurrentInning(_requestedInningNumber);
	}

	public void LoadGame(Guid gameId, string context, int inningNumber)
	{
		_gameViewModel.LoadGame(gameId, context);
		if (inningNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(inningNumber), "Inning number must be positive.");
		}

		_requestedInningNumber = inningNumber;
		CurrentInning = GetOrCreateCurrentInning(inningNumber);
	}

	private PitchingInning GetOrCreateCurrentInning(int? inningNumber = null)
	{
		var game = _gameViewModel.RequireCurrentGame();
		_gameId = game.Id;
		if (inningNumber is not null)
		{
			var requestedInning = game.PitchingInnings.FirstOrDefault(inning =>
				inning.InningNumber == inningNumber.Value);
			if (requestedInning is not null)
			{
				return requestedInning;
			}

			if (inningNumber.Value != game.PitchingInnings.Count + 1)
			{
				throw new KeyNotFoundException($"Inning {inningNumber.Value} does not exist in the current game.");
			}

			var newInning = new PitchingInning { InningNumber = inningNumber.Value };
			game.PitchingInnings.Add(newInning);
			_gameViewModel.SaveCurrentGame();
			return newInning;
		}

		if (game.PitchingInnings.Count == 0)
		{
			var firstInning = new PitchingInning { InningNumber = 1 };
			game.PitchingInnings.Add(firstInning);
			_gameViewModel.SaveCurrentGame();
			return firstInning;
		}

		return game.PitchingInnings[^1];
	}

	private void RecordHitResult(PitchingEventType type)
	{
		if (_selectedAction != PitchAction.Hit || _selectedPitchType is null)
		{
			throw new InvalidOperationException("Select Hit and a pitch type before choosing a hit result.");
		}

		RecordHit(type);
		HideChoices();
	}

	private void RecordPitch(
		PitchingEventType type,
		PitchType? pitchType,
		params PitchingEventType[] allowedTypes)
	{
		if (!allowedTypes.Contains(type))
		{
			throw new ArgumentOutOfRangeException(nameof(type), type, "This event is not valid for this action.");
		}

		CurrentInning.Events.Add(new PitchingEvent { Type = type, PitchType = pitchType });
		_gameViewModel.SaveCurrentGame();
	}

	private void HideChoices()
	{
		ShowPitchTypeOptions = false;
		ShowHitResultOptions = false;
		ShowOutTypeOptions = false;
		_selectedAction = null;
		_selectedPitchType = null;
	}

	public enum PitchAction
	{
		Strike,
		Ball,
		Hit
	}
}
