using Dadchanger.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dadchanger;

public partial class AppShell : Shell
{
	public const string MainRoute = "///mainpage";
	public const string PreviousGamesRoute = "///previousgames";
	public const string GameLogRoute = "///gamelog";
	public const string GameReportRoute = "///gamereport";
	public const string GameRoute = "///game";
	public const string PitchingRoute = "///pitching";
	public const string BattingRoute = "///batting";

	private readonly GameViewModel _gameViewModel;
	private readonly Stack<RouteSnapshot> _navigationHistory = new();
	private RouteSnapshot? _currentSnapshot;
	private bool _isNavigatingBack;

	public AppShell(IServiceProvider serviceProvider, GameViewModel gameViewModel)
	{
		_gameViewModel = gameViewModel;
		InitializeComponent();

		MainPageContent.ContentTemplate = CreatePageTemplate<MainPage>(serviceProvider);
		PreviousGamesPageContent.ContentTemplate = CreatePageTemplate<PreviousGamesPage>(serviceProvider);
		GameLogPageContent.ContentTemplate = CreatePageTemplate<GameLogPage>(serviceProvider);
		GameReportPageContent.ContentTemplate = CreatePageTemplate<GameReportPage>(serviceProvider);
		GamePageContent.ContentTemplate = CreatePageTemplate<GamePage>(serviceProvider);
		PitchingPageContent.ContentTemplate = CreatePageTemplate<PitchingPage>(serviceProvider);
		BattingPageContent.ContentTemplate = CreatePageTemplate<BattingPage>(serviceProvider);
		Navigated += OnShellNavigated;
	}

	protected override bool OnBackButtonPressed()
	{
		if (FlyoutIsPresented)
		{
			FlyoutIsPresented = false;
			return true;
		}

		if (_navigationHistory.TryPop(out var previous))
		{
			_ = NavigateBackAsync(previous);
			return true;
		}

		if (_currentSnapshot?.Route != "mainpage")
		{
			_ = NavigateBackAsync(new RouteSnapshot("mainpage"));
		}

		return true;
	}

	private void OnShellNavigated(object? sender, ShellNavigatedEventArgs args)
	{
		var snapshot = CaptureSnapshot(GetRouteName(args.Current.Location));
		if (!_isNavigatingBack && _currentSnapshot is { } current && !current.Equals(snapshot))
		{
			_navigationHistory.Push(current);
		}

		_currentSnapshot = snapshot;
	}

	private RouteSnapshot CaptureSnapshot(string route)
	{
		var game = _gameViewModel.CurrentGame;
		var snapshot = new RouteSnapshot(
			route,
			game?.Id,
			_gameViewModel.Context,
			game is null ? null : _gameViewModel.CurrentInningNumber,
			game is null ? null : _gameViewModel.CurrentAtBatNumber);

		switch (route)
		{
			case "pitching" when CurrentPage?.BindingContext is PitchingViewModel pitchingViewModel:
				snapshot = snapshot with { InningNumber = pitchingViewModel.CurrentInning.InningNumber };
				break;
			case "batting" when CurrentPage?.BindingContext is BattingViewModel battingViewModel:
				snapshot = snapshot with { AtBatNumber = battingViewModel.CurrentAtBat.AtBatNumber };
				break;
			case "gamereport" when CurrentPage?.BindingContext is GameReportViewModel reportViewModel &&
				reportViewModel.Game is { } reportedGame:
				snapshot = snapshot with
				{
					GameId = reportedGame.Id,
					Context = reportViewModel.Context
				};
				break;
		}

		return snapshot;
	}

	private async Task NavigateBackAsync(RouteSnapshot snapshot)
	{
		try
		{
			_isNavigatingBack = true;
			switch (snapshot.Route)
			{
				case "mainpage":
					await GoToAsync(MainRoute);
					break;
				case "previousgames":
					await GoToAsync(PreviousGamesRoute);
					break;
				case "gamelog":
					await GoToAsync(GameLogRoute);
					break;
				case "game" when snapshot.GameId is { } gameId:
					await GoToAsync(GameRoute, new Dictionary<string, object>
					{
						["gameId"] = gameId,
						["context"] = snapshot.Context ?? "live"
					});
					break;
				case "pitching" when snapshot.GameId is { } gameId:
					await GoToAsync(PitchingRoute, new Dictionary<string, object>
					{
						["gameId"] = gameId,
						["context"] = snapshot.Context ?? "live",
						["inningNumber"] = snapshot.InningNumber ?? 1
					});
					break;
				case "batting" when snapshot.GameId is { } gameId:
					await GoToAsync(BattingRoute, new Dictionary<string, object>
					{
						["gameId"] = gameId,
						["context"] = snapshot.Context ?? "live",
						["atBatNumber"] = snapshot.AtBatNumber ?? 1
					});
					break;
				case "gamereport" when snapshot.GameId is { } gameId:
					await GoToAsync(GameReportRoute, new Dictionary<string, object>
					{
						["gameId"] = gameId,
						["context"] = snapshot.Context ?? "history"
					});
					break;
				default:
					await GoToAsync(MainRoute);
					break;
			}
		}
		catch (Exception exception)
		{
			await DisplayAlertAsync("Navigation error", exception.Message, "OK");
		}
		finally
		{
			_isNavigatingBack = false;
		}
	}

	private static string GetRouteName(Uri location) =>
		location.OriginalString
			.Split('?', '#')[0]
			.TrimEnd('/')
			.Split('/')
			.LastOrDefault() ?? string.Empty;

	private sealed record RouteSnapshot(
		string Route,
		Guid? GameId = null,
		string? Context = null,
		int? InningNumber = null,
		int? AtBatNumber = null);

	private static DataTemplate CreatePageTemplate<TPage>(IServiceProvider serviceProvider)
		where TPage : Page
	{
		return new DataTemplate(() => serviceProvider.GetRequiredService<TPage>());
	}
}
