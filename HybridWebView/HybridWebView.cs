using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        public string MainFile { get; set; }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            InitializeHybridWebView(MainFile);
        }

        partial void InitializeHybridWebView(string mainFileAssetPath);

        public virtual void OnMessageReceived(string message)
        {
            Debug.WriteLine($"Web Message Received: {message}");
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
