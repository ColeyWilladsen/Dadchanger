using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class GameReportPage : ContentPage, IQueryAttributable
{
	private readonly GameReportViewModel _viewModel;

	public GameReportPage(GameReportViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (!query.ContainsKey("gameId") && !query.ContainsKey("context"))
		{
			if (!_viewModel.LoadLatestGame())
			{
				_ = ShowNoGamesMessageAsync();
			}

			return;
		}

		var (gameId, context) = GameNavigationQuery.Read(query);
		_viewModel.LoadGame(gameId, context);
	}

	private async Task ShowNoGamesMessageAsync()
	{
		await DisplayAlertAsync("No game reports", "Record a game before opening a report.", "OK");
		await Shell.Current.GoToAsync(AppShell.MainRoute);
	}
}
