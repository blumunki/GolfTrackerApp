namespace GolfTrackerApp.Mobile;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		
#if ANDROID
		// Configure BlazorWebView for Android
		blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
#endif
	}
	
#if ANDROID
	private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
	{
		// Configure WebView settings for better compatibility
		if (e.WebView is Android.Webkit.WebView androidWebView)
		{
			androidWebView.Settings.JavaScriptEnabled = true;
			androidWebView.Settings.DomStorageEnabled = true;
			androidWebView.Settings.DatabaseEnabled = true;
			androidWebView.Settings.SetSupportMultipleWindows(false);
			
			// Enable debugging for development
#if DEBUG
			Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
		}
	}
#endif
}
