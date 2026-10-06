namespace Dadchanger;

internal static class GameNavigationQuery
{
	public static (Guid GameId, string Context) Read(IDictionary<string, object> query)
	{
		if (!query.TryGetValue("gameId", out var rawGameId) ||
			!Guid.TryParse(rawGameId?.ToString(), out var gameId))
		{
			throw new ArgumentException("A valid gameId query parameter is required.", nameof(query));
		}

		if (!query.TryGetValue("context", out var rawContext) ||
			string.IsNullOrWhiteSpace(rawContext?.ToString()))
		{
			throw new ArgumentException("A non-empty context query parameter is required.", nameof(query));
		}

		return (gameId, rawContext.ToString()!);
	}

	public static int ReadRequiredInt(IDictionary<string, object> query, string key)
	{
		if (!query.TryGetValue(key, out var rawValue) ||
			!int.TryParse(rawValue?.ToString(), out var value) ||
			value < 1)
		{
			throw new ArgumentException($"A positive {key} query parameter is required.", nameof(query));
		}

		return value;
	}
}
