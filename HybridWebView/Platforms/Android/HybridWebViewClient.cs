using Android.Webkit;
using Microsoft.Maui.Platform;
using System.Text;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    public class HybridWebViewClient : MauiWebViewClient
    {
        private readonly HybridWebViewHandler _handler;

        public HybridWebViewClient(HybridWebViewHandler handler) : base(handler)
        {
            _handler = handler;
        }

        public override WebResourceResponse ShouldInterceptRequest(AWebView view, IWebResourceRequest request)
        {
            if (new Uri(request.Url.ToString()) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
            {
                var relativePath = HybridWebView.AppOriginUri.MakeRelativeUri(uri).ToString().Replace('/', '\\');

                string contentType;
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = ((HybridWebView)_handler.VirtualView).MainFile;
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

                var assetPath = Path.Combine(((HybridWebView)_handler.VirtualView).HybridAssetRoot, relativePath);

                var contentStream = PlatformOpenAppPackageFile(assetPath);
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

        Stream PlatformOpenAppPackageFile(string filename)
        {
            filename = FileSystemUtils.NormalizePath(filename);

            try
            {
                return _handler.Context.Assets.Open(filename);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return null;
            }
        }

        static partial class FileSystemUtils
        {
            public static string NormalizePath(string filename) =>
                filename
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
        }
        private protected static IDictionary<string, string> GetHeaders(string contentType) =>
            new Dictionary<string, string> {
                { "Content-Type", contentType },
            };

        public override bool ShouldOverrideUrlLoading(AWebView view, IWebResourceRequest request)
        {
            return true;
        }
    }
}
