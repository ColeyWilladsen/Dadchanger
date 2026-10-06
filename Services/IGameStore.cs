using Dadchanger.Models;

namespace Dadchanger.Services;

public interface IGameStore
{
	Game StartNewGame();

	void EndGame(Game game);

	IReadOnlyList<Game> GetGames();

	Game? GetGameById(Guid id);

	void UpdateGame(Game game);
}
