using Dadchanger.Models;

namespace Dadchanger.Services;

public class GameStore : IGameStore
{
	private readonly List<Game> _games = [];
	private readonly object _lock = new();

	public Game StartNewGame()
	{
		var game = new Game
		{
			StartTime = DateTime.Now
		};

		lock (_lock)
		{
			_games.Add(game);
		}

		return game;
	}

	public void EndGame(Game game)
	{
		ArgumentNullException.ThrowIfNull(game);

		lock (_lock)
		{
			var index = FindGameIndex(game.Id);
			game.EndTime = DateTime.Now;
			_games[index] = game;
		}
	}

	public IReadOnlyList<Game> GetGames()
	{
		lock (_lock)
		{
			return _games.ToList();
		}
	}

	public Game? GetGameById(Guid id)
	{
		lock (_lock)
		{
			return _games.FirstOrDefault(game => game.Id == id);
		}
	}

	public void UpdateGame(Game game)
	{
		ArgumentNullException.ThrowIfNull(game);

		lock (_lock)
		{
			_games[FindGameIndex(game.Id)] = game;
		}
	}

	private int FindGameIndex(Guid id)
	{
		var index = _games.FindIndex(game => game.Id == id);
		if (index < 0)
		{
			throw new KeyNotFoundException($"No game with id '{id}' exists in the game store.");
		}

		return index;
	}
}
