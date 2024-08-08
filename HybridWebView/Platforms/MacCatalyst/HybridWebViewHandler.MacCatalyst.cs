using Foundation;
using Intents;
using Microsoft.Maui.Platform;
using System;
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
                var url = urlSchemeTask.Request.Url?.AbsoluteString ?? "";

                var responseData = await GetResponseBytes(url);

                if (responseData.StatusCode == 200)
                {
                    using (var dic = new NSMutableDictionary<NSString, NSString>())
                    {
                        dic.Add((NSString)"Content-Length", (NSString)(responseData.ResponseBytes.Length.ToString(CultureInfo.InvariantCulture)));
                        dic.Add((NSString)"Content-Type", (NSString)responseData.ContentType);

                        if (responseData.CustomHeaders != null)
                        {
                            foreach (var header in responseData.CustomHeaders)
                            {
                                // Add custom headers to the response. Skip the Content-Length and Content-Type headers.
                                if (header.Key != "Content-Length" && header.Key != "Content-Type")
                                {
                                    dic.Add((NSString)header.Key, (NSString)header.Value);
                                }
                            }
                        }
                        
                        //Ensure that the Cache-Control header is not set in the custom headers.
                        if(responseData.CustomHeaders == null || responseData.CustomHeaders.ContainsKey("Cache-Control"))
                        {
                            // Disable local caching. This will prevent user scripts from executing correctly.
                            dic.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                        }

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

            private async Task<(byte[] ResponseBytes, string ContentType, int StatusCode, IDictionary<string, string>? CustomHeaders)> GetResponseBytes(string? url)
            {
                //string contentType;

                string? originalUrl = url;
                url = QueryStringHelper.RemovePossibleQueryString(url);

                if (!string.IsNullOrEmpty(originalUrl) && new Uri(url) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
                {
                    var hwv = (HybridWebView)_webViewHandler.VirtualView;
                    PathUtils.GetRelativePathAndContentType(HybridWebView.AppOriginUri, uri, originalUrl, hwv.MainFile, out string relativePath, out string contentType, out string fullUrl);
                    
                    Stream? contentStream = null;
                    IDictionary<string, string>? customHeaders = null;

                    // Check to see if the request is a proxy request.
                    if (relativePath == HybridWebView.ProxyRequestPath)
                    {
                        var args = new HybridWebViewProxyEventArgs(fullUrl, contentType);
                        await hwv.OnProxyRequestMessage(args);

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

                    if (contentStream is not null)
                    {
                        using var ms = new MemoryStream();
                        contentStream.CopyTo(ms);
                        return (ms.ToArray(), contentType, StatusCode: 200, CustomHeaders: customHeaders);
                    }

                    string bundleRootDir = Path.Combine(NSBundle.MainBundle.ResourcePath, hwv.HybridAssetRoot ?? "");
                    string assetPath = Path.Combine(bundleRootDir, relativePath);

                    if (File.Exists(assetPath))
                    {
                        return (File.ReadAllBytes(assetPath), contentType, StatusCode: 200, CustomHeaders: null);
                    }
                }

                return (Array.Empty<byte>(), ContentType: string.Empty, StatusCode: 404, CustomHeaders: null);
            }

            [Export("webView:stopURLSchemeTask:")]
            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
            }
        }
    }
}
