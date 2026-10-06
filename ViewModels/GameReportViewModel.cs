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
				OnPropertyChanged(nameof(StrikeCount));
				OnPropertyChanged(nameof(BallCount));
				OnPropertyChanged(nameof(WalkCount));
				OnPropertyChanged(nameof(StrikeoutCount));
				OnPropertyChanged(nameof(HitsAllowed));
				OnPropertyChanged(nameof(OutCount));
			}
		}
	}

	public ObservableCollection<PitchingSummaryItem> PitchingSummaryItems { get; } = [];

	public ObservableCollection<BattingSummaryItem> BattingSummaryItems { get; } = [];

	public int InningsPitched => Game?.PitchingInnings.Count ?? 0;

	public int StrikeCount => Game?.CountStrikes() ?? 0;

	public int BallCount => Game?.CountBalls() ?? 0;

	public int WalkCount => Game?.AtBats.Count(atBat => atBat.IsWalk) ?? 0;

	public int StrikeoutCount => Game?.AtBats.Count(atBat => atBat.IsStrikeout) ?? 0;

	public int HitsAllowed => Game?.PitchingInnings
		.SelectMany(inning => inning.Events)
		.Count(IsHit) ?? 0;

	public int OutCount => Game?.PitchingInnings
		.SelectMany(inning => inning.Events)
		.Count(IsOut) ?? 0;

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
		Game = _gameStore.GetGameById(gameId)
			?? throw new KeyNotFoundException($"No game with id '{gameId}' exists in the game store.");
		Context = context;

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
		OnPropertyChanged(nameof(StrikeCount));
		OnPropertyChanged(nameof(BallCount));
		OnPropertyChanged(nameof(WalkCount));
		OnPropertyChanged(nameof(StrikeoutCount));
		OnPropertyChanged(nameof(HitsAllowed));
		OnPropertyChanged(nameof(OutCount));
	}

	private static bool IsHit(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.HitSingle or
			PitchingEventType.HitDouble or
			PitchingEventType.HitTriple;

	private static bool IsOut(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.HitOutGround or
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
			PitchingEventType.StrikeChangeup);
		Balls = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup);
		Hits = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.HitSingle or
			PitchingEventType.HitDouble or
			PitchingEventType.HitTriple);
		Outs = inning.Events.Count(pitchingEvent => pitchingEvent.Type is
			PitchingEventType.HitOutGround or
			PitchingEventType.HitOutFly);
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
