using Foundation;
using WebKit;

namespace HybridWebView
{
    partial class HybridWebView
    {
        internal const string AppHostAddress = "0.0.0.0";

        internal const string AppOrigin = "app://" + AppHostAddress + "/";
        internal static readonly Uri AppOriginUri = new(AppOrigin);

        partial void InitializeHybridWebView()
        {
            var wv = (WKWebView)Handler.PlatformView;

            using var nsUrl = new NSUrl(AppOrigin);
            using var request = new NSUrlRequest(nsUrl);
            wv.LoadRequest(request);
        }
    }
}
