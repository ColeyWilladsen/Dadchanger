using Dadchanger.Services;
using Dadchanger.ViewModels;
using Microsoft.Extensions.Logging;

namespace Dadchanger;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IGameStore, GameStore>();
		builder.Services.AddSingleton<IHapticsService, HapticsService>();
		builder.Services.AddSingleton<GameViewModel>();
		builder.Services.AddTransient<AppShell>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<PreviousGamesPage>();
		builder.Services.AddTransient<GameReportPage>();
		builder.Services.AddTransient<GamePage>();
		builder.Services.AddTransient<PitchingPage>();
		builder.Services.AddTransient<BattingPage>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<PreviousGamesViewModel>();
		builder.Services.AddTransient<GameReportViewModel>();
		builder.Services.AddTransient<PitchingViewModel>();
		builder.Services.AddTransient<BattingViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
