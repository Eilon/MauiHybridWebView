using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage.Streams;

namespace HybridWebView
{
    partial class HybridWebView
    {
        // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
        // making it substantially faster. Note that this isn't real HTTP traffic, since
        // we intercept all the requests within this origin.
        private static readonly string AppHostAddress = "0.0.0.0";

        /// <summary>
        /// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
        /// </summary>
        private static readonly string AppOrigin = $"https://{AppHostAddress}/";

        private static readonly Uri AppOriginUri = new(AppOrigin);

        private CoreWebView2Environment? _coreWebView2Environment;

        private Microsoft.UI.Xaml.Controls.WebView2 PlatformWebView => (Microsoft.UI.Xaml.Controls.WebView2)Handler!.PlatformView!;

        private partial async Task InitializeHybridWebView()
        {
            PlatformWebView.WebMessageReceived += Wv2_WebMessageReceived;

            _coreWebView2Environment = await CoreWebView2Environment.CreateAsync();

            await PlatformWebView.EnsureCoreWebView2Async();

            PlatformWebView.CoreWebView2.Settings.AreDevToolsEnabled = EnableWebDevTools;
            PlatformWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            PlatformWebView.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);
            PlatformWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        }

        private partial void NavigateCore(string url)
        {
            PlatformWebView.Source = new Uri(new Uri(AppOriginUri, url).ToString());
        }

        private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs eventArgs)
        {
            // Get a deferral object so that WebView2 knows there's some async stuff going on. We call Complete() at the end of this method.
            using var deferral = eventArgs.GetDeferral();
            
            var requestUri = QueryStringHelper.RemovePossibleQueryString(eventArgs.Request.Uri);
            
            if (new Uri(requestUri) is Uri uri && AppOriginUri.IsBaseOf(uri))
            {
                PathUtils.GetRelativePathAndContentType(HybridWebView.AppOriginUri, uri, eventArgs.Request.Uri, MainFile, out string? relativePath, out string contentType, out string fullUrl);
                
                Stream? contentStream = null;
                IDictionary<string, string>? customHeaders = null;

                // Check to see if the request is a proxy request
                if (relativePath == ProxyRequestPath)
                {
                    var args = new HybridWebViewProxyEventArgs(fullUrl, contentType);
                    await OnProxyRequestMessage(args);

                    if (args.ResponseStream != null)
                    {
                        contentType = args.ResponseContentType ?? PathUtils.PlanTextMimeType;
                        contentStream = args.ResponseStream;
                        customHeaders = args.CustomResponseHeaders;
                    }
                }

                if (contentStream is null)
                {
                    contentStream = KnownStaticFileProvider.GetKnownResourceStream(relativePath!);
                }

                if (contentStream is null)
                {
                    var assetPath = Path.Combine(HybridAssetRoot!, relativePath!);
                    contentStream = await GetAssetStreamAsync(assetPath);
                }

                if (contentStream is null)
                {
                    var notFoundContent = "Resource not found (404)";
                    eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(
                        Content: null,
                        StatusCode: 404,
                        ReasonPhrase: "Not Found",
                        Headers: GetHeaderString("text/plain", notFoundContent.Length, null)
                    );
                }
                else
                {
                    eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(
                        Content: await CopyContentToRandomAccessStreamAsync(contentStream),
                        StatusCode: 200,
                        ReasonPhrase: "OK",
                        Headers: GetHeaderString(contentType, (int)contentStream.Length, customHeaders)
                    );
                }

                contentStream?.Dispose();
            }

            // Notify WebView2 that the deferred (async) operation is complete and we set a response.
            deferral.Complete();

            async Task<IRandomAccessStream> CopyContentToRandomAccessStreamAsync(Stream content)
            {
                using var memStream = new MemoryStream();
                await content.CopyToAsync(memStream);
                var randomAccessStream = new InMemoryRandomAccessStream();
                await randomAccessStream.WriteAsync(memStream.GetWindowsRuntimeBuffer());
                return randomAccessStream;
            }
        }

        private protected static string GetHeaderString(string contentType, int contentLength, IDictionary<string, string>? customHeaders)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Content-Type: {contentType}");
            sb.AppendLine($"Content-Length: {contentLength}");

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    // Add custom headers to the response. Skip the Content-Length and Content-Type headers.
                    if (header.Key != "Content-Length" && header.Key != "Content-Type")
                    {
                        sb.AppendLine($"{header.Key}: {header.Value}");
                    }
                }
            }

            // Ensure that the Cache-Control header is not set in the custom headers.
            if (customHeaders == null || !customHeaders.ContainsKey("Cache-Control"))
            {
                // Disable local caching. This will prevent user scripts from executing correctly.
                sb.AppendLine("Cache-Control: no-cache, max-age=0, must-revalidate, no-store");
            }

            return sb.ToString();
        }

        private void Wv2_WebMessageReceived(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            OnMessageReceived(args.TryGetWebMessageAsString());
        }
    }
}
