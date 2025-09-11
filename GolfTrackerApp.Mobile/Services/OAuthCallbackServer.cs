using System.Net;
using System.Text;

namespace GolfTrackerApp.Mobile.Services;

public class OAuthCallbackServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly int _port;
    private readonly string _callbackPath;
    private TaskCompletionSource<string?>? _authCodeCompletionSource;

    public OAuthCallbackServer(int port = 7777, string callbackPath = "/oauth/callback")
    {
        _port = port;
        _callbackPath = callbackPath;
    }

    public async Task<string?> StartAndWaitForCallbackAsync(TimeSpan timeout)
    {
        try
        {
            _authCodeCompletionSource = new TaskCompletionSource<string?>();
            _cancellationTokenSource = new CancellationTokenSource(timeout);
            
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}{_callbackPath}/");
            _listener.Start();

            // Start listening for requests
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested && _listener.IsListening)
                    {
                        var context = await _listener.GetContextAsync();
                        await HandleCallbackAsync(context);
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException || ex is HttpListenerException)
                {
                    // Callback server stopped
                }
                catch (Exception ex)
                {
                    _authCodeCompletionSource?.TrySetException(ex);
                }
            }, _cancellationTokenSource.Token);

            // Wait for either the auth code or timeout
            using (_cancellationTokenSource.Token.Register(() => _authCodeCompletionSource.TrySetCanceled()))
            {
                return await _authCodeCompletionSource.Task;
            }
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            Stop();
        }
    }

    private async Task HandleCallbackAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // Extract authorization code from query parameters
            var query = request.Url?.Query;
            string? authCode = null;
            string? error = null;

            if (!string.IsNullOrEmpty(query))
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(query);
                authCode = queryParams["code"];
                error = queryParams["error"];
            }

            // Prepare response HTML
            string responseHtml;
            if (!string.IsNullOrEmpty(error))
            {
                responseHtml = $"<html><head><title>OAuth Error</title></head><body><h1>Authentication Error</h1><p>Error: {error}</p><p>You can close this window.</p></body></html>";
                response.StatusCode = 400;
            }
            else if (!string.IsNullOrEmpty(authCode))
            {
                responseHtml = "<html><head><title>Authentication Successful</title><meta name='viewport' content='width=device-width, initial-scale=1'><style>body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; text-align: center; padding: 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; min-height: 100vh; margin: 0; display: flex; flex-direction: column; justify-content: center; } .success { font-size: 32px; margin-bottom: 20px; } .instructions { font-size: 18px; line-height: 1.6; opacity: 0.9; } .redirect-message { background: rgba(255,255,255,0.1); padding: 20px; border-radius: 12px; margin: 20px 0; } .countdown { font-size: 24px; color: #28a745; font-weight: bold; }</style></head><body><div class='success'>🏌️ Golf Tracker</div><div class='success'>✓ Authentication Successful!</div><div class='instructions'><div class='redirect-message'><p>Returning to Golf Tracker app...</p><p>If the app doesn't open automatically, tap the back button or close this browser.</p></div></div><script>setTimeout(() => { window.location = 'golftracker://'; setTimeout(() => { window.close(); }, 500); }, 500);</script></body></html>";
                response.StatusCode = 200;
            }
            else
            {
                responseHtml = "<html><head><title>OAuth Callback</title></head><body><h1>No Authorization Code</h1><p>No authorization code received. You can close this window.</p></body></html>";
                response.StatusCode = 400;
            }

            // Send response
            var buffer = Encoding.UTF8.GetBytes(responseHtml);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            // Complete the task with the auth code
            _authCodeCompletionSource?.TrySetResult(authCode);
        }
        catch (Exception ex)
        {
            _authCodeCompletionSource?.TrySetException(ex);
        }
    }

    public void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch (Exception)
        {
            // Error stopping OAuth callback server
        }
    }

    public void Dispose()
    {
        Stop();
        _listener = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
