using Android.Webkit;
using Java.Time;
using Microsoft.Maui.Platform;
using System.Text;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    public class AndroidHybridWebViewClient : MauiWebViewClient
    {
        private readonly HybridWebViewHandler _handler;

        public AndroidHybridWebViewClient(HybridWebViewHandler handler) : base(handler)
        {
            _handler = handler;
        }

        public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
        {
            var originalUrl = request?.Url?.ToString();
            var requestUri = QueryStringHelper.RemovePossibleQueryString(originalUrl);

            var webView = (HybridWebView)_handler.VirtualView;

            if (!string.IsNullOrEmpty(originalUrl) && new Uri(requestUri) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
            {
                PathUtils.GetRelativePathAndContentType(HybridWebView.AppOriginUri, uri, originalUrl, webView.MainFile, out string? relativePath, out string contentType, out string fullUrl);
                
                Stream? contentStream = null;
                IDictionary<string, string>? customHeaders = null;

                // Check to see if the request is a proxy request.
                if (!string.IsNullOrEmpty(relativePath) && relativePath.Equals(HybridWebView.ProxyRequestPath))
                {
                    var args = new HybridWebViewProxyEventArgs(fullUrl, contentType);

                    // TODO: Don't block async. Consider making this an async call, and then calling DidFinish when done
                    webView.OnProxyRequestMessage(args).Wait();

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
                    var assetPath = Path.Combine(((HybridWebView)_handler.VirtualView).HybridAssetRoot!, relativePath!);
                    contentStream = PlatformOpenAppPackageFile(assetPath);
                }

                if (contentStream is null)
                {
                    var notFoundContent = "Resource not found (404)";

                    var notFoundByteArray = Encoding.UTF8.GetBytes(notFoundContent);
                    var notFoundContentStream = new MemoryStream(notFoundByteArray);

                    return new WebResourceResponse("text/plain", "UTF-8", 404, "Not Found", GetHeaders("text/plain", null), notFoundContentStream);
                }
                else
                {
                    // TODO: We don't know the content length because Android doesn't tell us. Seems to work without it!
                    return new WebResourceResponse(contentType, "UTF-8", 200, "OK", GetHeaders(contentType, customHeaders), contentStream);
                }
            }
            else
            {
                return base.ShouldInterceptRequest(view, request);
            }
        }

        private Stream? PlatformOpenAppPackageFile(string filename)
        {
            filename = PathUtils.NormalizePath(filename);

            try
            {
                return _handler.Context.Assets?.Open(filename);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return null;
            }
        }

        private protected static IDictionary<string, string> GetHeaders(string contentType, IDictionary<string, string>? customHeaders)
        {
            var headers = new Dictionary<string, string>();

            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    // Add custom headers to the response. Skip the Content-Length and Content-Type headers.
                    if (header.Key != "Content-Length" && header.Key != "Content-Type")
                    {
                        headers[header.Key] = header.Value;
                    }
                }
            }

            //If a custom header hasn't specified a content type, use the one we've determined.
            if (!headers.ContainsKey("Content-Type"))
            {
                headers.Add("Content-Type", contentType);
            }

            return headers;
        }
    }
}
