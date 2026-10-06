using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class BattingPage : ContentPage, IQueryAttributable
{
	private readonly BattingViewModel _viewModel;

	public BattingPage(BattingViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		var (gameId, context) = GameNavigationQuery.Read(query);
		_viewModel.LoadGame(gameId, context, GameNavigationQuery.ReadRequiredInt(query, "atBatNumber"));
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.StartAtBatIfNeeded();
	}
}
