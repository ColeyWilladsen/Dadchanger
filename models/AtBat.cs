namespace Dadchanger.Models;

public class AtBat
{
	private bool _isStrikeout;

	public int AtBatNumber { get; set; }

	public List<BattingEvent> Events { get; set; } = [];

	public bool IsWalk { get; set; }

	public bool IsStrikeout
	{
		get => _isStrikeout || CountStrikeEvents(this) >= 3;
		set => _isStrikeout = value;
	}

	public string Result { get; set; } = string.Empty;

	private static int CountStrikeEvents(AtBat atBat)
	{
		var strikes = 0;
		foreach (var battingEvent in atBat.Events)
		{
			switch (battingEvent.Type)
			{
				case BattingEventType.TakeStrike:
				case BattingEventType.SwingMiss:
					strikes++;
					break;
				case BattingEventType.SwingHitOutFoul when strikes < 2:
					strikes++;
					break;
			}
		}

		return strikes;
	}
}
