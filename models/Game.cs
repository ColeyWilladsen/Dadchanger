namespace Dadchanger.Models;

public class Game
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public string OpponentName { get; set; } = string.Empty;

	public int CurrentBalls { get; set; }

	public int CurrentStrikes { get; set; }

	public int ActiveInningNumber { get; set; } = 1;

	public List<AtBat> AtBats { get; set; } = [];

	public List<PitchingInning> PitchingInnings { get; set; } = [];

	public List<GameLogEntry> GameLogEntries { get; set; } = [];

	public int CountStrikes()
	{
		var totalPitchEvents = PitchingInnings.Sum(inning => inning.Events.Count) +
			AtBats.Sum(atBat => atBat.Events.Count);
		return totalPitchEvents - CountBalls();
	}

	public int CountBalls()
	{
		var pitchingBalls = PitchingInnings
			.SelectMany(inning => inning.Events)
			.Count(pitchingEvent => pitchingEvent.Type is
				PitchingEventType.BallFastball or
				PitchingEventType.BallCurveball or
				PitchingEventType.BallChangeup);

		var battingBalls = AtBats.Sum(atBat =>
			atBat.Events.Count(battingEvent => battingEvent.Type == BattingEventType.TakeBall));
		return pitchingBalls + battingBalls;
	}

	public int CountWalks()
	{
		return AtBats.Count(atBat => atBat.IsWalk) +
			PitchingInnings.Sum(CountPitchingWalks);
	}

	public int CountStrikeouts()
	{
		var pitchingStrikeouts = PitchingInnings.Sum(inning => inning.Events.Count(pitchingEvent =>
			pitchingEvent.CountsAsOut && pitchingEvent.CountsAsStrike));
		return AtBats.Count(atBat => atBat.IsStrikeout) + pitchingStrikeouts;
	}

	private static int CountPitchingWalks(PitchingInning inning)
	{
		var walks = 0;
		var balls = 0;

		foreach (var pitchingEvent in inning.Events)
		{
			if (IsPitchingBall(pitchingEvent))
			{
				balls++;
				if (pitchingEvent.CountsAsWalk || balls == 4)
				{
					walks++;
					balls = 0;
				}
			}
			else if (pitchingEvent.CountsAsOut ||
				pitchingEvent.Type is PitchingEventType.HitSingle or
					PitchingEventType.HitDouble or
					PitchingEventType.HitTriple or
					PitchingEventType.HitFoulBall or
					PitchingEventType.HitOutFoul or
					PitchingEventType.HitOutGround or
					PitchingEventType.HitOutFly)
			{
				balls = 0;
			}
		}

		return walks;
	}

	private static bool IsPitchingBall(PitchingEvent pitchingEvent) =>
		pitchingEvent.Type is PitchingEventType.BallFastball or
			PitchingEventType.BallCurveball or
			PitchingEventType.BallChangeup;
}
