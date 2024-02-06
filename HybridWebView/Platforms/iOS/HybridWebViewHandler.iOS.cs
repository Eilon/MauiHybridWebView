using Foundation;
using Microsoft.Maui.Platform;
using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;
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
            public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                var responseBytes = GetResponseBytes(urlSchemeTask.Request.Url?.AbsoluteString ?? "", out var contentType, statusCode: out var statusCode);
                if (statusCode == 200)
                {
                    using (var dic = new NSMutableDictionary<NSString, NSString>())
                    {
                        dic.Add((NSString)"Content-Length", (NSString)(responseBytes.Length.ToString(CultureInfo.InvariantCulture)));
                        dic.Add((NSString)"Content-Type", (NSString)contentType);
                        // Disable local caching. This will prevent user scripts from executing correctly.
                        dic.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                        if (urlSchemeTask.Request.Url != null)
                        {
                            using var response = new NSHttpUrlResponse(urlSchemeTask.Request.Url, statusCode, "HTTP/1.1", dic);
                            urlSchemeTask.DidReceiveResponse(response);
                        }

                    }
                    urlSchemeTask.DidReceiveData(NSData.FromArray(responseBytes));
                    urlSchemeTask.DidFinish();
                }
            }

            private byte[] GetResponseBytes(string? url, out string contentType, out int statusCode)
            {
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

                    if (fullUrl != null && fullUrl.ToLowerInvariant().StartsWith(HybridWebView.AppOrigin + "proxy?"))
                    {
                        var webView = _webViewHandler.VirtualView as HybridWebView;

                        if (webView != null)
                        {
                            var e = new HybridWebViewRestEventArgs(fullUrl);
                            webView.OnProxyRequestMessage(e).Wait();

                            if (e.ResponseStream != null)
                            {
                                contentType = e.ContentType ?? "text/plain";
                                contentStream = e.ResponseStream;
                            }
                        }
                    }

                    if (contentStream == null)
                    {
                        contentStream = KnownStaticFileProvider.GetKnownResourceStream(relativePath!);
                    }

                    if (contentStream is not null)
                    {
                        statusCode = 200;
                        using var ms = new MemoryStream();
                        contentStream.CopyTo(ms);
                        return ms.ToArray();
                    }

                    var assetPath = Path.Combine(bundleRootDir, relativePath);

                    if (File.Exists(assetPath))
                    {
                        statusCode = 200;
                        return File.ReadAllBytes(assetPath);
                    }
                }

                statusCode = 404;
                contentType = string.Empty;
                return Array.Empty<byte>();
            }

            [Export("webView:stopURLSchemeTask:")]
            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
            }
        }
    }
}
