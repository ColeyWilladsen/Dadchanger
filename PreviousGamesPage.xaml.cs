using Dadchanger.ViewModels;

namespace Dadchanger;

public partial class PreviousGamesPage : ContentPage, IQueryAttributable
{
	private readonly PreviousGamesViewModel _viewModel;

	public PreviousGamesPage(PreviousGamesViewModel viewModel)
	{
		_viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		_viewModel.RefreshGames();
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (!query.TryGetValue("context", out var rawContext) ||
			string.IsNullOrWhiteSpace(rawContext?.ToString()))
		{
			throw new ArgumentException("A non-empty context query parameter is required.", nameof(query));
		}

		_viewModel.LoadContext(rawContext.ToString()!);
	}
}
