namespace HybridWebView
{
    public class HybridWebViewResolveObjectEventArgs : EventArgs
    {
        public string ObjectName { get; init; }
        public HybridWebViewObjectHost Host { get; init; }
    }
}
