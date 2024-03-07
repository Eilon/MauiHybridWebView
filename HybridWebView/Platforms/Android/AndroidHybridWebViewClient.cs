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
            var fullUrl = request?.Url?.ToString();
            var requestUri = QueryStringHelper.RemovePossibleQueryString(fullUrl);

            var webView = (HybridWebView)_handler.VirtualView;

            if (new Uri(requestUri) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
            {
                var relativePath = HybridWebView.AppOriginUri.MakeRelativeUri(uri).ToString().Replace('/', '\\');

                string contentType;
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = webView.MainFile;
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

                // Check to see if the request is a proxy request.
                if (relativePath == HybridWebView.ProxyRequestPath)
                {
                    var args = new HybridWebViewProxyEventArgs(fullUrl);

                    // TODO: Don't block async. Consider making this an async call, and then calling DidFinish when done
                    webView.OnProxyRequestMessage(args).Wait();

                    if (args.ResponseStream != null)
                    {
                        contentType = args.ResponseContentType ?? "text/plain";
                        contentStream = args.ResponseStream;
                    }
                }

                if (contentStream == null)
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

                    return new WebResourceResponse("text/plain", "UTF-8", 404, "Not Found", GetHeaders("text/plain"), notFoundContentStream);
                }
                else
                {
                    // TODO: We don't know the content length because Android doesn't tell us. Seems to work without it!
                    return new WebResourceResponse(contentType, "UTF-8", 200, "OK", GetHeaders(contentType), contentStream);
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

        private protected static IDictionary<string, string> GetHeaders(string contentType) =>
            new Dictionary<string, string> {
                { "Content-Type", contentType },
            };
    }
}
