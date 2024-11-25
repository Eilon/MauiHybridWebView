using Foundation;
using Microsoft.Maui.Platform;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using WebKit;

namespace HybridWebView
{
    partial class HybridWebViewHandler
    {
        protected override WKWebView CreatePlatformView()
        {
            var config = new WKWebViewConfiguration();
            config.UserContentController.AddScriptMessageHandler(new WebViewScriptMessageHandler(MessageReceived), "webwindowinterop");
            config.SetUrlSchemeHandler(new SchemeHandler(this), urlScheme: "app");

            // Legacy Developer Extras setting.
            var enableWebDevTools = ((HybridWebView)VirtualView).EnableWebDevTools;
            config.Preferences.SetValueForKey(NSObject.FromObject(enableWebDevTools), new NSString("developerExtrasEnabled"));

            var platformView = new MauiWKWebView(RectangleF.Empty, this, config);

            if (OperatingSystem.IsMacCatalystVersionAtLeast(major: 13, minor: 3) ||
                OperatingSystem.IsIOSVersionAtLeast(major: 16, minor: 4))
            {
                // Enable Developer Extras for Catalyst/iOS builds for 16.4+
                platformView.SetValueForKey(NSObject.FromObject(enableWebDevTools), new NSString("inspectable"));
            }

            return platformView;
        }

        private void MessageReceived(Uri uri, string message)
        {
            ((HybridWebView)VirtualView).OnMessageReceived(message);
        }

        private sealed class WebViewScriptMessageHandler : NSObject, IWKScriptMessageHandler
        {
            private Action<Uri, string> _messageReceivedAction;

            public WebViewScriptMessageHandler(Action<Uri, string> messageReceivedAction)
            {
                _messageReceivedAction = messageReceivedAction ?? throw new ArgumentNullException(nameof(messageReceivedAction));
            }

            public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
            {
                if (message is null)
                {
                    throw new ArgumentNullException(nameof(message));
                }
                _messageReceivedAction(HybridWebView.AppOriginUri, ((NSString)message.Body).ToString());
            }
        }

        private class SchemeHandler : NSObject, IWKUrlSchemeHandler
        {
            private readonly HybridWebViewHandler _webViewHandler;

            public SchemeHandler(HybridWebViewHandler webViewHandler)
            {
                _webViewHandler = webViewHandler;
            }

            [Export("webView:startURLSchemeTask:")]
            [SupportedOSPlatform("ios11.0")]
            public async void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                var responseData = await GetResponseBytes(urlSchemeTask);

                if (responseData.StatusCode == 200)
                {
                    var keys = responseData.headers?.Keys?.Select(p => new NSString(p)) ?? Array.Empty<NSString>();
                    var values = responseData.headers?.Values?.Select(p => new NSString(p)) ?? Array.Empty<NSString>();

                    using (var dic = new NSMutableDictionary<NSString, NSString>(keys.ToArray(), values.ToArray()))
                    {
                        if (dic.ContainsKey((NSString)"Content-Length") == false)
                        {
                            dic.Add((NSString)"Content-Length", (NSString)(responseData.ResponseBytes.Length.ToString(CultureInfo.InvariantCulture)));
                        }

                        if (dic.ContainsKey((NSString)"Content-Type") == false)
                        {
                            dic.Add((NSString)"Content-Type", (NSString)responseData.ContentType);
                        }

                        // Disable local caching. This will prevent user scripts from executing correctly.
                        dic.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                        if (urlSchemeTask.Request.Url != null)
                        {
                            using var response = new NSHttpUrlResponse(urlSchemeTask.Request.Url, responseData.StatusCode, "HTTP/1.1", dic);
                            urlSchemeTask.DidReceiveResponse(response);
                        }
                    }

                    urlSchemeTask.DidReceiveData(NSData.FromArray(responseData.ResponseBytes));
                    urlSchemeTask.DidFinish();
                }
            }

            private async Task<(byte[] ResponseBytes, string ContentType, int StatusCode, IDictionary<string, string>? headers)> GetResponseBytes(IWKUrlSchemeTask urlSchemeTask)
            {
                var url = urlSchemeTask.Request.Url?.AbsoluteString ?? "";
                var method = urlSchemeTask.Request.HttpMethod;
                var requestHeaders = urlSchemeTask.Request.Headers?.ToDictionary(p => p.Key.ToString(), p => p.Value.ToString());

                string contentType;

                string fullUrl = url;
                url = QueryStringHelper.RemovePossibleQueryString(url);
                
                if (new Uri(url) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
                {
                    var relativePath = HybridWebView.AppOriginUri.MakeRelativeUri(uri).ToString().Replace('\\', '/');

                    var hwv = (HybridWebView)_webViewHandler.VirtualView;

                    var bundleRootDir = Path.Combine(NSBundle.MainBundle.ResourcePath, hwv.HybridAssetRoot!);

                    if (string.IsNullOrEmpty(relativePath))
                    {
                        relativePath = hwv.MainFile!.Replace('\\', '/');
                        contentType = "text/html";
                    }
                    else
                    {
                        var requestExtension = Path.GetExtension(relativePath);
                        contentType = requestExtension switch
                        {
                            ".htm" or ".html" => "text/html",
                            ".js" => "application/javascript",
                            ".css" => "text/css",
                            _ => "text/plain",
                        };
                    }

                    Stream? contentStream = null;
                    IDictionary<string, string>? responseHeaders = null;

                    // Check to see if the request is a proxy request.
                    if (relativePath == HybridWebView.ProxyRequestPath)
                    {
                        var args = new HybridWebViewProxyEventArgs(fullUrl, method, requestHeaders);

                        await hwv.OnProxyRequestMessage(args);

                        if (args.ResponseStream != null)
                        {
                            contentType = args.ResponseContentType ?? "text/plain";
                            contentStream = args.ResponseStream;
                            responseHeaders = args.ResponseHeaders;
                        }
                    }

                    if (contentStream == null)
                    {
                        contentStream = KnownStaticFileProvider.GetKnownResourceStream(relativePath!);
                    }

                    if (contentStream is not null)
                    {
                        using var ms = new MemoryStream();
                        contentStream.CopyTo(ms);
                        return (ms.ToArray(), contentType, StatusCode: 200, responseHeaders);
                    }

                    var assetPath = Path.Combine(bundleRootDir, relativePath);

                    if (File.Exists(assetPath))
                    {
                        return (File.ReadAllBytes(assetPath), contentType, StatusCode: 200, responseHeaders);
                    }
                }

                return (Array.Empty<byte>(), ContentType: string.Empty, StatusCode: 404, null);
            }

            [Export("webView:stopURLSchemeTask:")]
            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
            }
        }
    }
}
