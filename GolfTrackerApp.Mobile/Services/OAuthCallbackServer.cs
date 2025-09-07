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

            Console.WriteLine($"DEBUG: OAuth callback server started on http://localhost:{_port}{_callbackPath}");

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
                    Console.WriteLine($"DEBUG: Callback server stopped: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Callback server error: {ex.Message}");
                    _authCodeCompletionSource?.TrySetException(ex);
                }
            }, _cancellationTokenSource.Token);

            // Wait for either the auth code or timeout
            using (_cancellationTokenSource.Token.Register(() => _authCodeCompletionSource.TrySetCanceled()))
            {
                return await _authCodeCompletionSource.Task;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Failed to start OAuth callback server: {ex.Message}");
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

            Console.WriteLine($"DEBUG: Callback received: {request.Url}");

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
                responseHtml = "<html><head><title>Authentication Successful</title><meta name='viewport' content='width=device-width, initial-scale=1'><style>body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; text-align: center; padding: 40px; } .success { color: #28a745; font-size: 24px; margin-bottom: 20px; } .instructions { color: #6c757d; font-size: 16px; line-height: 1.5; }</style></head><body><div class='success'>✓ Authentication Successful!</div><div class='instructions'><p>You have been successfully authenticated with Google.</p><p><strong>Please tap 'Cancel' or 'Done' to return to the Golf Tracker app.</strong></p><p>This window will close automatically.</p></div><script>setTimeout(() => { document.body.innerHTML = '<div style=\"color: #28a745; font-size: 18px; padding: 40px;\">✓ Success! Please return to the Golf Tracker app.</div>'; }, 3000);</script></body></html>";
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

            Console.WriteLine($"DEBUG: Auth code extracted: {authCode?.Substring(0, Math.Min(10, authCode?.Length ?? 0))}...");

            // Complete the task with the auth code
            _authCodeCompletionSource?.TrySetResult(authCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Error handling callback: {ex.Message}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Error stopping OAuth callback server: {ex.Message}");
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
