namespace HybridWebView
{
    /// <summary>
    /// Event arg object for a proxy request from the hybrid webview.
    /// </summary>
    public class HybridWebViewProxyEventArgs
    {
        /// <summary>
        /// Event arg object for a proxy request from the hybrid webview.
        /// </summary>
        /// <param name="fullUrl">The full request URL.</param>
        public HybridWebViewProxyEventArgs(string fullUrl)
        {
            RequestUrl = fullUrl;
            QueryParams = QueryStringHelper.GetKeyValuePairs(fullUrl);
        }

        /// <summary>
        /// The full request URL.
        /// </summary>
        public string RequestUrl { get; }

        /// <summary>
        /// Query string values extracted from the request URL.
        /// </summary>
        public Dictionary<string, string> QueryParams { get; }

        /// <summary>
        /// The response content type.
        /// </summary>

        public string? ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// The response stream to be used to respond to the request.
        /// </summary>
        public Stream? ResponseStream { get; set; } = null;
    }
}
