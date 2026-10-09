namespace Dadchanger.Models;

public class PitchingEvent
{
	public PitchingEventType Type { get; set; }

	public PitchType? PitchType { get; set; }

	public bool CountsAsStrike { get; set; }

	public bool CountsAsOut { get; set; }

	public bool CountsAsWalk { get; set; }
}

public enum PitchType
{
	Fastball,
	Curveball,
	Changeup
}

public enum PitchingEventType
{
	StrikeFastball,
	StrikeCurveball,
	StrikeChangeup,
	BallFastball,
	BallCurveball,
	BallChangeup,
	HitSingle,
	HitDouble,
	HitTriple,
	HitFoulBall,
	HitOutGround,
	HitOutFly,
	HitOutFoul
}
