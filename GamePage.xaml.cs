using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class GamePage : ContentPage, IQueryAttributable
{
	private readonly GameViewModel _viewModel;

	public GamePage(GameViewModel viewModel)
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
