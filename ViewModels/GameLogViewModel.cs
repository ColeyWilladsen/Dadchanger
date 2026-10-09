using System.Collections.ObjectModel;
using Dadchanger.Models;

namespace Dadchanger.ViewModels;

public sealed class GameLogViewModel : ViewModelBase
{
	private readonly GameViewModel _gameViewModel;
	private string _emptyMessage = "Start a game to see its log.";

	public ObservableCollection<GameLogEntry> Entries { get; } = [];

	public string EmptyMessage
	{
		get => _emptyMessage;
		private set => SetProperty(ref _emptyMessage, value);
	}

	public GameLogViewModel(GameViewModel gameViewModel)
	{
		_gameViewModel = gameViewModel;
	}

	public void Refresh()
	{
		Entries.Clear();
		var game = _gameViewModel.CurrentGame;
		if (game is null)
		{
			EmptyMessage = "Start a game to see its log.";
			return;
		}

		foreach (var entry in game.GameLogEntries)
		{
			Entries.Add(entry);
		}

		EmptyMessage = "No pitches or batting actions have been recorded yet.";
	}
}
