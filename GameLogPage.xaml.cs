using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class GameLogPage : ContentPage
{
	private readonly GameLogViewModel _viewModel;

	public GameLogPage(GameLogViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.Refresh();
	}
}
