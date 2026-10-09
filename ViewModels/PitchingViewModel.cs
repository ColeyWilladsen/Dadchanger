using System.Collections.ObjectModel;
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

	public PitchingInning CurrentInning
	{
		get => _currentInning;
		private set => SetProperty(ref _currentInning, value);
	}

	public ObservableCollection<byte> BallDots { get; } = [];

	public ObservableCollection<byte> StrikeDots { get; } = [];

	public ObservableCollection<byte> OutMarkers { get; } = [];

	public int Balls => _gameViewModel.RequireCurrentGame().CurrentBalls;

	public int Strikes => _gameViewModel.RequireCurrentGame().CurrentStrikes;

	public int Outs => CurrentInning.Events.Count(IsOut);

	public int PitchesThrown => _gameViewModel.RequireCurrentGame().PitchingInnings
		.Sum(inning => inning.Events.Count);

	public ICommand StrikeCommand { get; }
	public ICommand BallCommand { get; }
	public ICommand HitCommand { get; }
	public ICommand RunScoredCommand { get; }
	public ICommand EndInningCommand { get; }

	public PitchingViewModel(GameViewModel gameViewModel, IHapticsService hapticsService)
	{
		_gameViewModel = gameViewModel;
		_hapticsService = hapticsService;
		_currentInning = GetOrCreateCurrentInning();

		StrikeCommand = new AsyncRelayCommand(() => SelectPitchAsync(PitchAction.Strike));
		BallCommand = new AsyncRelayCommand(() => SelectPitchAsync(PitchAction.Ball));
		HitCommand = new AsyncRelayCommand(() => SelectPitchAsync(PitchAction.Hit));
		RunScoredCommand = new AsyncRelayCommand(RecordRunScoredAsync);
		EndInningCommand = new AsyncRelayCommand(async () =>
		{
			_hapticsService.Tap();
			await EndInningAsync();
		});
	}

	private async Task SelectPitchAsync(PitchAction action)
	{
		_hapticsService.Tap();
		var selectedPitch = await Shell.Current.DisplayActionSheetAsync(
			"Select pitch type",
			"Cancel",
			null,
			"Fastball",
			"Curveball",
			"Changeup");

		var pitchType = selectedPitch switch
		{
			"Fastball" => PitchType.Fastball,
			"Curveball" => PitchType.Curveball,
			"Changeup" => PitchType.Changeup,
			_ => (PitchType?)null
		};
		if (pitchType is null)
		{
			return;
		}

		_hapticsService.Tap();
		switch (action)
		{
			case PitchAction.Strike:
				await RecordPitch(pitchType switch
				{
					PitchType.Fastball => PitchingEventType.StrikeFastball,
					PitchType.Curveball => PitchingEventType.StrikeCurveball,
					PitchType.Changeup => PitchingEventType.StrikeChangeup,
					_ => throw new ArgumentOutOfRangeException(nameof(selectedPitch))
				}, pitchType,
				PitchingEventType.StrikeFastball,
				PitchingEventType.StrikeCurveball,
				PitchingEventType.StrikeChangeup);
				break;
			case PitchAction.Ball:
				await RecordPitch(pitchType switch
				{
					PitchType.Fastball => PitchingEventType.BallFastball,
					PitchType.Curveball => PitchingEventType.BallCurveball,
					PitchType.Changeup => PitchingEventType.BallChangeup,
					_ => throw new ArgumentOutOfRangeException(nameof(selectedPitch))
				}, pitchType,
				PitchingEventType.BallFastball,
				PitchingEventType.BallCurveball,
				PitchingEventType.BallChangeup);
				break;
			case PitchAction.Hit:
				await SelectHitResultAsync(pitchType.Value);
				break;
		}
	}

	private async Task SelectHitResultAsync(PitchType pitchType)
	{
		var selectedResult = await Shell.Current.DisplayActionSheetAsync(
			"Select hit result",
			"Cancel",
			null,
			"Single",
			"Double",
			"Triple",
			"Foulball",
			"Out");

		var result = selectedResult switch
		{
			"Single" => PitchingEventType.HitSingle,
			"Double" => PitchingEventType.HitDouble,
			"Triple" => PitchingEventType.HitTriple,
			"Foulball" => PitchingEventType.HitFoulBall,
			"Out" => await SelectOutTypeAsync(),
			_ => (PitchingEventType?)null
		};

		if (result is null)
		{
			return;
		}

		_hapticsService.Tap();
		await RecordPitch(
			result.Value,
			pitchType,
			PitchingEventType.HitSingle,
			PitchingEventType.HitDouble,
			PitchingEventType.HitTriple,
			PitchingEventType.HitFoulBall,
			PitchingEventType.HitOutFoul,
			PitchingEventType.HitOutGround,
			PitchingEventType.HitOutFly);
	}

	private static async Task<PitchingEventType?> SelectOutTypeAsync()
	{
		var selectedOut = await Shell.Current.DisplayActionSheetAsync(
			"Select out type",
			"Cancel",
			null,
			"Foul Out",
			"Ground Out",
			"Fly Out");

		return selectedOut switch
		{
			"Foul Out" => PitchingEventType.HitOutFoul,
			"Ground Out" => PitchingEventType.HitOutGround,
			"Fly Out" => PitchingEventType.HitOutFly,
			_ => null
		};
	}

	public async Task EndInningAsync()
	{
		var game = _gameViewModel.RequireCurrentGame();
		var nextInningNumber = CurrentInning.Events.Count > 0 || CurrentInning.EarnedRuns > 0
			? CurrentInning.InningNumber + 1
			: CurrentInning.InningNumber;
		_gameViewModel.SetUpcomingInningNumber(nextInningNumber);
		_requestedInningNumber = nextInningNumber;
		CurrentInning = GetOrCreateCurrentInning(nextInningNumber);
		await Shell.Current.GoToAsync(
			AppShell.GameRoute,
			new Dictionary<string, object>
			{
				["gameId"] = game.Id,
				["context"] = "live",
				["inningNumber"] = nextInningNumber
			});
	}

	public void RefreshCurrentInning()
	{
		var game = _gameViewModel.RequireCurrentGame();
		_gameId = game.Id;
		CurrentInning = GetOrCreateCurrentInning(_requestedInningNumber);
		RefreshCountDisplay();
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

			return new PitchingInning { InningNumber = inningNumber.Value };
		}

		if (game.PitchingInnings.Count == 0)
		{
			return new PitchingInning { InningNumber = 1 };
		}

		return game.PitchingInnings[^1];
	}

	private async Task RecordPitch(
		PitchingEventType type,
		PitchType? pitchType,
		params PitchingEventType[] allowedTypes)
	{
		if (!allowedTypes.Contains(type))
		{
			throw new ArgumentOutOfRangeException(nameof(type), type, "This event is not valid for this action.");
		}

		var game = _gameViewModel.RequireCurrentGame();
		EnsureCurrentInningIsRecorded(game);

		var countsAsStrike = type is
			PitchingEventType.StrikeFastball or
			PitchingEventType.StrikeCurveball or
			PitchingEventType.StrikeChangeup;
		var countsAsWalk = false;
		var countsAsOut = type is
			PitchingEventType.HitOutFoul or
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly;
		var strikeNumber = 0;
		var ballNumber = 0;

		if (countsAsStrike)
		{
			game.CurrentStrikes++;
			strikeNumber = game.CurrentStrikes;
			if (game.CurrentStrikes == 3)
			{
				countsAsOut = true;
				game.CurrentBalls = 0;
				game.CurrentStrikes = 0;
			}
		}
		else if (type is
			PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup)
		{
			game.CurrentBalls++;
			ballNumber = game.CurrentBalls;
			if (game.CurrentBalls == 4)
			{
				countsAsWalk = true;
				game.CurrentBalls = 0;
				game.CurrentStrikes = 0;
			}
		}
		else if (type == PitchingEventType.HitFoulBall)
		{
			if (game.CurrentStrikes < 2)
			{
				game.CurrentStrikes++;
				countsAsStrike = true;
			}
		}
		else if (type is PitchingEventType.HitSingle or
			PitchingEventType.HitDouble or
			PitchingEventType.HitTriple or
			PitchingEventType.HitOutFoul or
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly)
		{
			game.CurrentBalls = 0;
			game.CurrentStrikes = 0;
		}

		var pitchingEvent = new PitchingEvent
		{
			Type = type,
			PitchType = pitchType,
			CountsAsStrike = countsAsStrike,
			CountsAsOut = countsAsOut,
			CountsAsWalk = countsAsWalk
		};
		CurrentInning.Events.Add(pitchingEvent);
		_gameViewModel.AddGameLogEntry(
			BuildPitchLogDescription(pitchingEvent, strikeNumber, ballNumber),
			inningNumber: CurrentInning.InningNumber);
		RefreshCountDisplay();
		if (Outs == 3)
		{
			await EndInningAsync();
		}
	}

	private Task RecordRunScoredAsync()
	{
		_hapticsService.Tap();
		var game = _gameViewModel.RequireCurrentGame();
		EnsureCurrentInningIsRecorded(game);
		CurrentInning.EarnedRuns++;
		_gameViewModel.AddGameLogEntry(
			"Run scored (earned run)",
			inningNumber: CurrentInning.InningNumber);
		return Task.CompletedTask;
	}

	private void EnsureCurrentInningIsRecorded(Game game)
	{
		if (game.PitchingInnings.Contains(CurrentInning))
		{
			return;
		}

		if (CurrentInning.InningNumber != game.PitchingInnings.Count + 1)
		{
			throw new InvalidOperationException("The current inning is not the next inning in this game.");
		}

		game.PitchingInnings.Add(CurrentInning);
	}

	private void RefreshCountDisplay()
	{
		OnPropertyChanged(nameof(Balls));
		OnPropertyChanged(nameof(Strikes));
		OnPropertyChanged(nameof(Outs));
		OnPropertyChanged(nameof(PitchesThrown));

		BallDots.Clear();
		for (var i = 0; i < Balls; i++)
		{
			BallDots.Add(0);
		}

		StrikeDots.Clear();
		for (var i = 0; i < Strikes; i++)
		{
			StrikeDots.Add(0);
		}

		OutMarkers.Clear();
		for (var i = 0; i < Outs; i++)
		{
			OutMarkers.Add(0);
		}
	}

	private static bool IsOut(PitchingEvent pitchingEvent) =>
		pitchingEvent.CountsAsOut ||
		pitchingEvent.Type is PitchingEventType.HitOutFoul or
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly;

	private string BuildPitchLogDescription(PitchingEvent pitchingEvent, int strikeNumber, int ballNumber)
	{
		var pitchName = pitchingEvent.PitchType?.ToString() ?? "Pitch";
		return pitchingEvent.Type switch
		{
			PitchingEventType.StrikeFastball or
			PitchingEventType.StrikeCurveball or
			PitchingEventType.StrikeChangeup =>
				$"{pitchName} thrown for strike {strikeNumber}" +
				(pitchingEvent.CountsAsOut ? " (strikeout)" : string.Empty),
			PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup =>
				$"{pitchName} thrown for ball {ballNumber}" +
				(pitchingEvent.CountsAsWalk ? " (walk)" : string.Empty),
			PitchingEventType.HitFoulBall when pitchingEvent.CountsAsStrike =>
				$"{pitchName} thrown for a foul ball (strike {Strikes})",
			PitchingEventType.HitFoulBall =>
				$"{pitchName} thrown for a foul ball (strike count remains {Strikes})",
			PitchingEventType.HitSingle => $"{pitchName} resulted in a single",
			PitchingEventType.HitDouble => $"{pitchName} resulted in a double",
			PitchingEventType.HitTriple => $"{pitchName} resulted in a triple",
			PitchingEventType.HitOutFoul => $"{pitchName} resulted in a foul out (out {Outs})",
			PitchingEventType.HitOutGround => $"{pitchName} resulted in a ground out (out {Outs})",
			PitchingEventType.HitOutFly => $"{pitchName} resulted in a fly out (out {Outs})",
			_ => throw new ArgumentOutOfRangeException(nameof(pitchingEvent))
		};
	}

	public enum PitchAction
	{
		Strike,
		Ball,
		Hit
	}
}
