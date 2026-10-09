namespace Dadchanger.Models;

public class GameLogEntry
{
	public DateTime Timestamp { get; set; }

	public int? InningNumber { get; set; }

	public int? AtBatNumber { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Context => InningNumber is { } inningNumber
		? $"Inning {inningNumber}"
		: AtBatNumber is { } atBatNumber
			? $"At Bat {atBatNumber}"
			: string.Empty;
}
