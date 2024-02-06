using System.ComponentModel;

namespace HybridWebView
{
    public class HybridWebViewRestEventArgs : EventArgs
    {
        public HybridWebViewRestEventArgs(Dictionary<string, string>? queryParams)
        {
            QueryParams = queryParams;
        }

        public Dictionary<string, string>? QueryParams { get; }

        public Stream? ResponseStream { get; set; }

        public string? ContentType { get; set; }
    }
}
