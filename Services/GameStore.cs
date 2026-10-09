using Dadchanger.Models;
using System.Text.Json;

namespace Dadchanger.Services;

public class GameStore : IGameStore
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true
	};

	private readonly List<Game> _games;
	private readonly object _lock = new();
	private readonly string _storagePath = Path.Combine(FileSystem.AppDataDirectory, "games.json");

	public GameStore()
	{
		_games = LoadGames();
	}

	public Game StartNewGame(string? opponentName = null)
	{
		var game = new Game
		{
			StartTime = DateTime.Now,
			OpponentName = opponentName?.Trim() ?? string.Empty
		};

		lock (_lock)
		{
			_games.Add(game);
			SaveGames();
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
			SaveGames();
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
			SaveGames();
		}
	}

	private List<Game> LoadGames()
	{
		if (!File.Exists(_storagePath))
		{
			return [];
		}

		return JsonSerializer.Deserialize<List<Game>>(
			File.ReadAllText(_storagePath),
			SerializerOptions)
			?? throw new InvalidDataException($"Game data in '{_storagePath}' is empty or invalid.");
	}

	private void SaveGames()
	{
		var temporaryPath = $"{_storagePath}.tmp";
		File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_games, SerializerOptions));
		File.Move(temporaryPath, _storagePath, overwrite: true);
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
