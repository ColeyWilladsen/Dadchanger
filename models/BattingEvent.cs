namespace Dadchanger.Models;

public class BattingEvent
{
	public BattingEventType Type { get; set; }
}

public enum BattingEventType
{
	TakeBall,
	TakeStrike,
	SwingHitSingle,
	SwingHitDouble,
	SwingHitTriple,
	SwingHitOutFoul,
	SwingHitOutGround,
	SwingHitOutFly,
	SwingMiss
}
