using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using GolfTrackerApp.Mobile.Services.Api;

namespace GolfTrackerApp.Mobile;

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

		builder.Services.AddMauiBlazorWebView();

		// Add MudBlazor services
		builder.Services.AddMudServices();

		// Configure HTTP client for API calls to web server
		builder.Services.AddHttpClient<IGolfClubApiService, GolfClubApiService>(client =>
		{
			// TODO: Make this configurable for different environments
			client.BaseAddress = new Uri("https://localhost:7295/");
			client.DefaultRequestHeaders.Add("Accept", "application/json");
		})
#if DEBUG
		.ConfigurePrimaryHttpMessageHandler(() =>
		{
			var handler = new HttpClientHandler();
			// Allow self-signed certificates in development
			handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
			return handler;
		})
#endif
		;

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		return app;
	}
}
