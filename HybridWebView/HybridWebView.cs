namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        public string MainFile { get; set; }
        public string HybridAssetRoot { get; set; }

        public event EventHandler<HybridWebViewMessageReceivedEventArgs> MessageReceived;

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            InitializeHybridWebView();
        }

        partial void InitializeHybridWebView();

        public virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new HybridWebViewMessageReceivedEventArgs(message));
        }

        internal static async Task<string> GetAssetContentAsync(string assetPath)
        {
            using var stream = await GetAssetStreamAsync(assetPath);
            if (stream == null)
            {
                return null;
            }
            using var reader = new StreamReader(stream);

            var contents = reader.ReadToEnd();

            return contents;
        }

        internal static async Task<Stream> GetAssetStreamAsync(string assetPath)
        {
            if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            {
                return null;
            }
            return await FileSystem.OpenAppPackageFileAsync(assetPath);
        }
    }
}
