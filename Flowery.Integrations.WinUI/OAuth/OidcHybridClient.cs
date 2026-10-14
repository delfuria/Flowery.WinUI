using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace Flowery.Integrations.WinUI.OAuth;

public sealed class OidcHybridClient
{
    private readonly OidcClientOptions _options;

    public OidcHybridClient(OidcClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<LoginResult> LoginAsync(CancellationToken cancellation = default)
    {
        var loopbackOptions = EnsureLoopbackOptions();
        var loopbackClient = new OidcClient(loopbackOptions);
        var loginRequest = new LoginRequest
        {
            FrontChannelExtraParameters = new Parameters
            {
                { "prompt", "login" }
            }
        };
        return await loopbackClient.LoginAsync(loginRequest, cancellationToken: cancellation);
    }

    private OidcClientOptions EnsureLoopbackOptions()
    {
        if (_options.Browser == null)
        {
            _options.Browser = new LoopbackBrowser();
        }

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            _options.RedirectUri = LoopbackBrowser.GetRedirectUri(7890);
        }

        if (string.IsNullOrWhiteSpace(_options.PostLogoutRedirectUri))
        {
            _options.PostLogoutRedirectUri = _options.RedirectUri;
        }

        if (_options.BrowserTimeout != TimeSpan.FromSeconds(30))
        {
            _options.BrowserTimeout = TimeSpan.FromSeconds(30);
        }

        return _options;
    }

    private sealed class LoopbackBrowser : IBrowser
    {
        private const string ResponseHtml = "<html><head><title>Flowery</title></head><body>You can return to the app.</body></html>";

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var endUrl = options.EndUrl;
            if (string.IsNullOrWhiteSpace(endUrl))
            {
                return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Missing redirect URL." };
            }

            var listener = new HttpListener();
            try
            {
                listener.IgnoreWriteExceptions = true;
                listener.Prefixes.Add(endUrl);
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    return new BrowserResult
                    {
                        ResultType = BrowserResultType.UnknownError,
                        Error = $"Failed to start loopback listener: {ex.Message}"
                    };
                }

                OpenBrowser(options.StartUrl);

                var timeout = options.Timeout;
                if (timeout <= TimeSpan.Zero)
                {
                    timeout = TimeSpan.FromMinutes(5);
                }

                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().WaitAsync(timeout);
                }
                catch (TimeoutException)
                {
                    return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = "Timed out waiting for the browser response." };
                }
                catch (Exception ex)
                {
                    return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
                }
                var request = context.Request;
                var response = context.Response;
                var resultUrl = request.Url?.AbsoluteUri;

                var buffer = Encoding.UTF8.GetBytes(ResponseHtml);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.KeepAlive = false;
                response.ContentType = "text/html; charset=utf-8";
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["Pragma"] = "no-cache";
                response.Headers["Connection"] = "close";
                try
                {
                    response.Close(buffer, willBlock: false);
                }
                catch (ObjectDisposedException)
                {
                }
                catch
                {
                }
                TryDrainExtraRequests(listener, buffer, cancellationToken);
                return new BrowserResult
                {
                    ResultType = BrowserResultType.Success,
                    Response = resultUrl
                };
            }
            finally
            {
                try
                {
                    listener.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoopbackBrowser listener close failed: {ex.Message}");
                }
            }
        }

        public static string GetRedirectUri(int port = 0)
        {
            if (port == 0)
            {
                port = GetRandomUnusedPort();
            }

            return $"http://127.0.0.1:{port}/";
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoopbackBrowser failed to launch browser: {ex.Message}");
            }
        }

        private static void TryDrainExtraRequests(HttpListener listener, byte[] responseBuffer, CancellationToken token)
        {
            // Some browsers issue a follow-up request (e.g., favicon) after the callback.
            // Keep the listener alive briefly to avoid ERR_CONNECTION_REFUSED.
            const int maxExtraRequests = 1;
            var remaining = maxExtraRequests;
            while (remaining-- > 0)
            {
                try
                {
                    var contextTask = listener.GetContextAsync();
                    var context = contextTask.WaitAsync(TimeSpan.FromSeconds(1), token).GetAwaiter().GetResult();
                    var response = context.Response;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.StatusDescription = "OK";
                    response.KeepAlive = false;
                    response.ContentType = "text/html; charset=utf-8";
                    response.Headers["Cache-Control"] = "no-store";
                    response.Headers["Pragma"] = "no-cache";
                    response.Headers["Connection"] = "close";
                    response.Close(responseBuffer, willBlock: false);
                }
                catch
                {
                    // Ignore any follow-up failures.
                    break;
                }
            }
        }
    }
}
