namespace Dadchanger.Models;

public class Game
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public string OpponentName { get; set; } = string.Empty;

	public List<AtBat> AtBats { get; set; } = [];

	public List<PitchingInning> PitchingInnings { get; set; } = [];

	public int CountStrikes()
	{
		return PitchingInnings
			.SelectMany(inning => inning.Events)
			.Count(pitchingEvent => pitchingEvent.Type is
				PitchingEventType.StrikeFastball or
				PitchingEventType.StrikeCurveball or
				PitchingEventType.StrikeChangeup);
	}

	public int CountBalls()
	{
		return PitchingInnings
			.SelectMany(inning => inning.Events)
			.Count(pitchingEvent => pitchingEvent.Type is
				PitchingEventType.BallFastball or
				PitchingEventType.BallCurveball or
				PitchingEventType.BallChangeup);
	}

	public int CountWalks()
	{
		return AtBats.Count(atBat => atBat.IsWalk);
	}

	public int CountStrikeouts()
	{
		return AtBats.Count(atBat => atBat.IsStrikeout);
	}
}
