using Android.Webkit;
using Java.Interop;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    partial class HybridWebView
    {
        private HybridWebViewJavaScriptInterface _javaScriptInterface;
        async partial void InitializeHybridWebView(string mainFileAssetPath)
        {
            var awv = (AWebView)Handler.PlatformView;
            awv.Settings.JavaScriptEnabled = true;

            _javaScriptInterface = new HybridWebViewJavaScriptInterface(this);
            awv.AddJavascriptInterface(_javaScriptInterface, "hybridWebViewHost");
            awv.LoadDataWithBaseURL(
                baseUrl: "https://0.0.0.0/",
                data: await GetAssetContentAsync(mainFileAssetPath),
                mimeType: "text/html",
                encoding: "UTF-8",
                historyUrl: null);
        }

        private sealed class HybridWebViewJavaScriptInterface : Java.Lang.Object
        {
            private readonly HybridWebView _hybridWebView;

            public HybridWebViewJavaScriptInterface(HybridWebView hybridWebView)
            {
                _hybridWebView = hybridWebView;
            }

            [JavascriptInterface]
            [Export("sendMessage")]
            public void SendMessage(string message)
            {
                _hybridWebView.OnMessageReceived(message);
            }
        }
    }
}
