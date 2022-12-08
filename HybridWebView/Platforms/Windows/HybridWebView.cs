namespace HybridWebView
{
    partial class HybridWebView
    {
        async partial void InitializeHybridWebView(string mainFileAssetPath)
        {
            var wv2 = (Microsoft.UI.Xaml.Controls.WebView2)Handler.PlatformView;
            wv2.WebMessageReceived += Wv2_WebMessageReceived;
            await wv2.EnsureCoreWebView2Async();

            wv2.CoreWebView2.Settings.IsWebMessageEnabled = true;

            wv2.NavigateToString(await GetAssetContentAsync(mainFileAssetPath));
        }

        private void Wv2_WebMessageReceived(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            OnMessageReceived(args.TryGetWebMessageAsString());
        }
    }
}
