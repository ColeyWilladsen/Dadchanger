using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class PitchingPage : ContentPage, IQueryAttributable
{
	private readonly PitchingViewModel _viewModel;

	public PitchingPage(PitchingViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		var (gameId, context) = GameNavigationQuery.Read(query);
		_viewModel.LoadGame(gameId, context, GameNavigationQuery.ReadRequiredInt(query, "inningNumber"));
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.RefreshCurrentInning();
	}
}
