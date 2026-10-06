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
		var (gameId, context) = GameNavigationQuery.Read(query);
		_viewModel.LoadGame(gameId, context);
	}
}
