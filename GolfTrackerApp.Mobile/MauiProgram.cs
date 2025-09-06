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

		// Configure base HTTP client settings
		var httpClientBuilder = new Action<HttpClient>(client =>
		{
			// TODO: Make this configurable for different environments
			client.BaseAddress = new Uri("https://localhost:7295/");
			client.DefaultRequestHeaders.Add("Accept", "application/json");
		});

#if DEBUG
		var httpMessageHandlerFactory = new Func<HttpMessageHandler>(() =>
		{
			var handler = new HttpClientHandler();
			// Allow self-signed certificates in development
			handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
			return handler;
		});
#endif

		// Configure HTTP clients for API services
		builder.Services.AddHttpClient<IGolfClubApiService, GolfClubApiService>(httpClientBuilder)
#if DEBUG
			.ConfigurePrimaryHttpMessageHandler(httpMessageHandlerFactory)
#endif
			;

		builder.Services.AddHttpClient<IPlayerApiService, PlayerApiService>(httpClientBuilder)
#if DEBUG
			.ConfigurePrimaryHttpMessageHandler(httpMessageHandlerFactory)
#endif
			;

		builder.Services.AddHttpClient<IRoundApiService, RoundApiService>(httpClientBuilder)
#if DEBUG
			.ConfigurePrimaryHttpMessageHandler(httpMessageHandlerFactory)
#endif
			;

		builder.Services.AddHttpClient<IDashboardApiService, DashboardApiService>(httpClientBuilder)
#if DEBUG
			.ConfigurePrimaryHttpMessageHandler(httpMessageHandlerFactory)
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
