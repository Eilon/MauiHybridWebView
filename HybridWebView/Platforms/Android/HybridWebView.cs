using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    partial class HybridWebView
    {
        partial void InitializeHybridWebView(string mainFileAssetPath)
        {
            var awv = (AWebView)Handler.PlatformView;
        }
    }
}
