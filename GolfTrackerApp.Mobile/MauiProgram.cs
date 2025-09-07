using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using GolfTrackerApp.Mobile.Services.Api;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Configuration;

namespace GolfTrackerApp.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		
		// Add configuration support for user secrets - manual approach
#if DEBUG
		try
		{
			// Manual user secrets loading for MAUI
			var userSecretsId = "225a16cb-90ae-40d8-b215-93fc8db227ba";
			var userSecretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", userSecretsId, "secrets.json");
			
			System.Diagnostics.Debug.WriteLine($"Looking for user secrets at: {userSecretsPath}");
			
			if (File.Exists(userSecretsPath))
			{
				builder.Configuration.AddJsonFile(userSecretsPath, optional: true);
				System.Diagnostics.Debug.WriteLine("User secrets file found and loaded");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("User secrets file not found");
			}
			
			// Also try the standard approach as backup
			builder.Configuration.AddUserSecrets("225a16cb-90ae-40d8-b215-93fc8db227ba");
			
			// Temporary workaround: Add configuration values directly for testing
			var configValues = new Dictionary<string, string?>
			{
                ["Authentication:Google:ClientId"] = “[[GOOGLECLIENTIDNEEDSREPLACING]]“,
                ["Authentication:Google:ClientSecret"] = “[[GOOGLECLIENTSECRETNEEDSREPLACING]]“
			};
			builder.Configuration.AddInMemoryCollection(configValues);
			System.Diagnostics.Debug.WriteLine("Added in-memory configuration values");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error setting up user secrets: {ex.Message}");
		}
#endif

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

		// Authentication services
		builder.Services.AddSingleton<AuthenticationStateService>();
		builder.Services.AddHttpClient<GoogleAuthenticationService>(client =>
		{
			client.BaseAddress = new Uri("https://localhost:7295/");
			client.DefaultRequestHeaders.Add("Accept", "application/json");
		})
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
