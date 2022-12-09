using Microsoft.Maui.Controls;

namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        public string MainFile { get; set; }

        public event EventHandler<HybridWebViewMessageReceivedEventArgs> MessageReceived;

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            InitializeHybridWebView(MainFile);
        }

        partial void InitializeHybridWebView(string mainFileAssetPath);

        public virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new HybridWebViewMessageReceivedEventArgs(message));
        }

        private async Task<string> GetAssetContentAsync(string assetPath)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            using var reader = new StreamReader(stream);

            var contents = reader.ReadToEnd();

            return contents;
        }
    }
}
