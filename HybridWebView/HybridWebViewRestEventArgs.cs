using System.ComponentModel;

namespace HybridWebView
{
    public class HybridWebViewRestEventArgs : EventArgs
    {
        public HybridWebViewRestEventArgs(string fullUrl)
        {
            FullUrl = fullUrl;

            if (fullUrl != null)
            {
                QueryParams = new Uri(fullUrl).Query
                          .Substring(1)
                          .Split('&')
                          .Select(p => p.Split('='))
                          .ToDictionary(p => p[0], p => p[1]);
            }
        }

        public string FullUrl { get; }

        public Dictionary<string, string>? QueryParams { get; }

        public Stream? ResponseStream { get; set; }

        public string? ContentType { get; set; }
    }
}
