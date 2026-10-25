using System.Collections.ObjectModel;
using Dadchanger.Models;
using Dadchanger.Services;

namespace Dadchanger.ViewModels;

public sealed class GameReportViewModel : ViewModelBase
{
	private readonly IGameStore _gameStore;
	private Game? _game;
	private string _context = string.Empty;

	public Game? Game
	{
		get => _game;
		private set
		{
			if (SetProperty(ref _game, value))
			{
				OnPropertyChanged(nameof(InningsPitched));
				OnPropertyChanged(nameof(PitchingStrikeCount));
				OnPropertyChanged(nameof(PitchingBallCount));
				OnPropertyChanged(nameof(PitchingWalkCount));
				OnPropertyChanged(nameof(PitchingStrikeoutCount));
				OnPropertyChanged(nameof(BattingAtBatCount));
				OnPropertyChanged(nameof(BattingStrikeCount));
				OnPropertyChanged(nameof(BattingBallCount));
				OnPropertyChanged(nameof(BattingWalkCount));
				OnPropertyChanged(nameof(BattingStrikeoutCount));
				OnPropertyChanged(nameof(BattingHitCount));
				OnPropertyChanged(nameof(HitsAllowed));
				OnPropertyChanged(nameof(OutCount));
				OnPropertyChanged(nameof(EarnedRunsAllowed));
				OnPropertyChanged(nameof(EarnedRunAverage));
				OnPropertyChanged(nameof(TotalPitchCount));
				OnPropertyChanged(nameof(FastballPitchCount));
				OnPropertyChanged(nameof(CurveballPitchCount));
				OnPropertyChanged(nameof(ChangeupPitchCount));
				OnPropertyChanged(nameof(FastballPercentage));
				OnPropertyChanged(nameof(CurveballPercentage));
				OnPropertyChanged(nameof(ChangeupPercentage));
				OnPropertyChanged(nameof(PitchingStrikePercentage));
				OnPropertyChanged(nameof(PitchingBallPercentage));
				OnPropertyChanged(nameof(BattingStrikePercentage));
				OnPropertyChanged(nameof(BattingBallPercentage));
				OnPropertyChanged(nameof(FirstPitchStrikePercentage));
			}
		}
	}

	public ObservableCollection<PitchingSummaryItem> PitchingSummaryItems { get; } = [];

	public ObservableCollection<BattingSummaryItem> BattingSummaryItems { get; } = [];

	public string InningsPitched => FormatInnings(OutCount);

	public int PitchingStrikeCount
	{
		get
		{
			var events = GetPitchingEvents();
			return events.Count - events.Count(IsBallOutcome);
		}
	}

	public int PitchingBallCount => GetPitchingEvents().Count(IsBallOutcome);

	public int PitchingWalkCount => Game?.PitchingInnings.Sum(CountPitchingWalks) ?? 0;

	public int PitchingStrikeoutCount => GetPitchingEvents().Count(pitchingEvent =>
		pitchingEvent.CountsAsOut && pitchingEvent.CountsAsStrike);

	public int BattingAtBatCount => Game?.AtBats.Count ?? 0;

	public int BattingBallCount => Game?.AtBats.Sum(atBat =>
		atBat.Events.Count(battingEvent => battingEvent.Type == BattingEventType.TakeBall)) ?? 0;

	public int BattingStrikeCount => (Game?.AtBats.Sum(atBat => atBat.Events.Count) ?? 0) -
		BattingBallCount;

	public int BattingWalkCount => Game?.AtBats.Count(atBat => atBat.IsWalk) ?? 0;

	public int BattingStrikeoutCount => Game?.AtBats.Count(atBat => atBat.IsStrikeout) ?? 0;

	public int BattingHitCount => Game?.AtBats.Sum(atBat =>
		atBat.Events.Count(battingEvent => battingEvent.Type is
			BattingEventType.SwingHitSingle or
			BattingEventType.SwingHitDouble or
			BattingEventType.SwingHitTriple)) ?? 0;

	public int HitsAllowed => Game?.PitchingInnings
		.SelectMany(inning => inning.Events)
		.Count(IsHit) ?? 0;

	public int OutCount => Game?.PitchingInnings
		.SelectMany(inning => inning.Events)
		.Count(IsOut) ?? 0;

	public int EarnedRunsAllowed => Game?.PitchingInnings.Sum(inning => inning.EarnedRuns) ?? 0;

	public string EarnedRunAverage => OutCount == 0
		? "N/A"
		: (EarnedRunsAllowed * 27d / OutCount).ToString("F2");

	public int TotalPitchCount => GetPitchingEvents().Count;

	public int FastballPitchCount => GetPitchCount(PitchType.Fastball);

	public int CurveballPitchCount => GetPitchCount(PitchType.Curveball);

	public int ChangeupPitchCount => GetPitchCount(PitchType.Changeup);

	public double FastballPercentage => GetPitchPercentage(PitchType.Fastball);

	public double CurveballPercentage => GetPitchPercentage(PitchType.Curveball);

	public double ChangeupPercentage => GetPitchPercentage(PitchType.Changeup);

	public double PitchingStrikePercentage
	{
		get
		{
			var total = PitchingStrikeCount + PitchingBallCount;
			return total == 0 ? 0 : PitchingStrikeCount * 100d / total;
		}
	}

	public double PitchingBallPercentage
	{
		get
		{
			var total = PitchingStrikeCount + PitchingBallCount;
			return total == 0 ? 0 : PitchingBallCount * 100d / total;
		}
	}

	public double BattingStrikePercentage
	{
		get
		{
			var total = BattingStrikeCount + BattingBallCount;
			return total == 0 ? 0 : BattingStrikeCount * 100d / total;
		}
	}

	public double BattingBallPercentage
	{
		get
		{
			var total = BattingStrikeCount + BattingBallCount;
			return total == 0 ? 0 : BattingBallCount * 100d / total;
		}
	}

	public double FirstPitchStrikePercentage
	{
		get
		{
			var pitchingInnings = Game?.PitchingInnings
				.OrderBy(inning => inning.InningNumber)
				.ToList() ?? [];
			if (pitchingInnings.Any(inning => inning.Events.Count > 0))
			{
				var firstPitchCount = 0;
				var firstPitchStrikeCount = 0;

				foreach (var inning in pitchingInnings)
				{
					var atBatHasPitch = false;
					var balls = 0;
					var strikes = 0;

					foreach (var pitchingEvent in inning.Events)
					{
						if (!atBatHasPitch)
						{
							firstPitchCount++;
							if (IsFirstPitchStrike(pitchingEvent))
							{
								firstPitchStrikeCount++;
							}

							atBatHasPitch = true;
						}

						if (IsBallOutcome(pitchingEvent))
						{
							balls++;
							if (balls == 4)
							{
								atBatHasPitch = false;
								balls = 0;
								strikes = 0;
							}
						}
						else if (IsCountedStrike(pitchingEvent))
						{
							strikes++;
							if (strikes == 3)
							{
								atBatHasPitch = false;
								balls = 0;
								strikes = 0;
							}
						}

						if (IsHit(pitchingEvent) || IsOut(pitchingEvent))
						{
							atBatHasPitch = false;
							balls = 0;
							strikes = 0;
						}
					}
				}

				return firstPitchCount == 0 ? 0 : firstPitchStrikeCount * 100d / firstPitchCount;
			}

			var atBats = Game?.AtBats.Where(atBat => atBat.Events.Count > 0).ToList() ?? [];
			var battingFirstPitchStrikes = atBats.Count(atBat =>
				IsFirstBattingEventStrike(atBat.Events[0]));
			return atBats.Count == 0 ? 0 : battingFirstPitchStrikes * 100d / atBats.Count;
		}
	}

	public string Context
	{
		get => _context;
		private set => SetProperty(ref _context, value);
	}

	public GameReportViewModel(IGameStore gameStore, IHapticsService hapticsService)
	{
		_gameStore = gameStore;
	}

	public void LoadGame(Guid gameId, string context)
	{
		var game = _gameStore.GetGameById(gameId)
			?? throw new KeyNotFoundException($"No game with id '{gameId}' exists in the game store.");
		LoadGame(game, context);
	}

	public bool LoadLatestGame()
	{
		var game = _gameStore.GetGames()
			.OrderByDescending(game => game.StartTime)
			.FirstOrDefault();
		if (game is null)
		{
			return false;
		}

		LoadGame(game, game.EndTime == default ? "live" : "history");
		return true;
	}

	private void LoadGame(Game game, string context)
	{
		Context = context;
		Game = game;

		PitchingSummaryItems.Clear();
		foreach (var inning in Game.PitchingInnings.OrderBy(inning => inning.InningNumber))
		{
			PitchingSummaryItems.Add(new PitchingSummaryItem(inning));
		}

		BattingSummaryItems.Clear();
		foreach (var atBat in Game.AtBats.OrderBy(atBat => atBat.AtBatNumber))
		{
			BattingSummaryItems.Add(new BattingSummaryItem(atBat));
		}

		OnPropertyChanged(nameof(InningsPitched));
		OnPropertyChanged(nameof(PitchingStrikeCount));
		OnPropertyChanged(nameof(PitchingBallCount));
		OnPropertyChanged(nameof(PitchingWalkCount));
		OnPropertyChanged(nameof(PitchingStrikeoutCount));
		OnPropertyChanged(nameof(BattingAtBatCount));
		OnPropertyChanged(nameof(BattingStrikeCount));
		OnPropertyChanged(nameof(BattingBallCount));
		OnPropertyChanged(nameof(BattingWalkCount));
		OnPropertyChanged(nameof(BattingStrikeoutCount));
		OnPropertyChanged(nameof(BattingHitCount));
		OnPropertyChanged(nameof(HitsAllowed));
		OnPropertyChanged(nameof(OutCount));
		OnPropertyChanged(nameof(EarnedRunsAllowed));
		OnPropertyChanged(nameof(EarnedRunAverage));
		OnPropertyChanged(nameof(TotalPitchCount));
		OnPropertyChanged(nameof(FastballPitchCount));
		OnPropertyChanged(nameof(CurveballPitchCount));
		OnPropertyChanged(nameof(ChangeupPitchCount));
		OnPropertyChanged(nameof(FastballPercentage));
		OnPropertyChanged(nameof(CurveballPercentage));
		OnPropertyChanged(nameof(ChangeupPercentage));
		OnPropertyChanged(nameof(PitchingStrikePercentage));
		OnPropertyChanged(nameof(PitchingBallPercentage));
		OnPropertyChanged(nameof(BattingStrikePercentage));
		OnPropertyChanged(nameof(BattingBallPercentage));
		OnPropertyChanged(nameof(FirstPitchStrikePercentage));
	}

	private int GetPitchCount(PitchType pitchType) => GetPitchingEvents()
		.Count(pitchingEvent => pitchingEvent.PitchType == pitchType);

	private static int CountPitchingWalks(PitchingInning inning)
	{
		var walks = 0;
		var balls = 0;

		foreach (var pitchingEvent in inning.Events)
		{
			if (IsBallOutcome(pitchingEvent))
			{
				balls++;
				if (pitchingEvent.CountsAsWalk || balls == 4)
				{
					walks++;
					balls = 0;
				}
			}
			else if (pitchingEvent.CountsAsOut || IsHit(pitchingEvent))
			{
				balls = 0;
			}
		}

		return walks;
	}

	private double GetPitchPercentage(PitchType pitchType)
	{
		var totalPitchCount = TotalPitchCount;
		return totalPitchCount == 0 ? 0 : GetPitchCount(pitchType) * 100d / totalPitchCount;
	}

	private static string FormatInnings(int outs)
	{
		var completeInnings = outs / 3;
		var remainingOuts = outs % 3;
		return remainingOuts switch
		{
			0 => completeInnings.ToString(),
			1 => $"{completeInnings} 1/3",
			_ => $"{completeInnings} 2/3"
		};
	}

	private List<PitchingEvent> GetPitchingEvents() => Game?.PitchingInnings
		.OrderBy(inning => inning.InningNumber)
		.SelectMany(inning => inning.Events)
		.ToList() ?? [];

	private int GetBattingEventCount() => Game?.AtBats.Sum(atBat => atBat.Events.Count) ?? 0;

	private static bool IsBallOutcome(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup;

	private static bool IsFirstPitchStrike(PitchingEvent pitchingEvent) =>
		!IsBallOutcome(pitchingEvent);

	private static bool IsCountedStrike(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.StrikeFastball or
			PitchingEventType.StrikeCurveball or
			PitchingEventType.StrikeChangeup ||
		pitchingEvent.CountsAsStrike;

	private static bool IsFirstBattingEventStrike(BattingEvent battingEvent) =>
		battingEvent.Type is not BattingEventType.TakeBall;

	private static bool IsHit(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.HitSingle or
			PitchingEventType.HitDouble or
			PitchingEventType.HitTriple;

	private static bool IsOut(PitchingEvent pitchingEvent) =>
		pitchingEvent.CountsAsOut ||
		pitchingEvent.Type is PitchingEventType.HitOutFoul or
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly;
}

public sealed class PitchingSummaryItem
{
	public int InningNumber { get; }

	public int Strikes { get; }

	public int Balls { get; }

	public int Hits { get; }

	public int Outs { get; }

	public PitchingSummaryItem(PitchingInning inning)
	{
		InningNumber = inning.InningNumber;
		Strikes = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
				PitchingEventType.StrikeFastball or
				PitchingEventType.StrikeCurveball or
				PitchingEventType.StrikeChangeup ||
			pitchingEvent.CountsAsStrike);
		Balls = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup);
		Hits = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.HitSingle or
			PitchingEventType.HitDouble or
			PitchingEventType.HitTriple);
		Outs = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.HitOutFoul or
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly ||
			pitchingEvent.CountsAsOut);
	}
}

public sealed class BattingSummaryItem
{
	public int AtBatNumber { get; }

	public string Result { get; }

	public IReadOnlyList<BattingEvent> Events { get; }

	public BattingSummaryItem(AtBat atBat)
	{
		AtBatNumber = atBat.AtBatNumber;
		Result = string.IsNullOrWhiteSpace(atBat.Result) ? "In progress" : atBat.Result;
		Events = atBat.Events.ToList();
	}
}
