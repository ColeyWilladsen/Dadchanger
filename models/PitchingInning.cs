namespace Dadchanger.Models;

public class PitchingInning
{
	public int InningNumber { get; set; }

	public List<PitchingEvent> Events { get; set; } = [];
}
