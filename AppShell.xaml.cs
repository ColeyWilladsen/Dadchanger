using Microsoft.Extensions.DependencyInjection;

namespace Dadchanger;

public partial class AppShell : Shell
{
	public const string MainRoute = "///mainpage";
	public const string PreviousGamesRoute = "///previousgames";
	public const string GameReportRoute = "///gamereport";
	public const string GameRoute = "///game";
	public const string PitchingRoute = "///pitching";
	public const string BattingRoute = "///batting";

	public AppShell(IServiceProvider serviceProvider)
	{
		InitializeComponent();

		MainPageContent.ContentTemplate = CreatePageTemplate<MainPage>(serviceProvider);
		PreviousGamesPageContent.ContentTemplate = CreatePageTemplate<PreviousGamesPage>(serviceProvider);
		GameReportPageContent.ContentTemplate = CreatePageTemplate<GameReportPage>(serviceProvider);
		GamePageContent.ContentTemplate = CreatePageTemplate<GamePage>(serviceProvider);
		PitchingPageContent.ContentTemplate = CreatePageTemplate<PitchingPage>(serviceProvider);
		BattingPageContent.ContentTemplate = CreatePageTemplate<BattingPage>(serviceProvider);
	}

	private static DataTemplate CreatePageTemplate<TPage>(IServiceProvider serviceProvider)
		where TPage : Page
	{
		return new DataTemplate(() => serviceProvider.GetRequiredService<TPage>());
	}
}
