namespace Dadchanger.Models;

public class AtBat
{
	public int AtBatNumber { get; set; }

	public List<BattingEvent> Events { get; set; } = [];

	public bool IsWalk { get; set; }

	public bool IsStrikeout { get; set; }

	public string Result { get; set; } = string.Empty;
}
