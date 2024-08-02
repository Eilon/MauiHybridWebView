namespace HybridWebView
{
    /// <summary>
    /// Event arg object for a proxy request from the <see cref="HybridWebView"/>.
    /// </summary>
    public class HybridWebViewProxyEventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="HybridWebViewProxyEventArgs"/>.
        /// </summary>
        /// <param name="fullUrl">The full request URL.</param>
        public HybridWebViewProxyEventArgs(string fullUrl)
        {
            Url = fullUrl;
            QueryParams = QueryStringHelper.GetKeyValuePairs(fullUrl);
        }

        /// <summary>
        /// Creates a new instance of <see cref="HybridWebViewProxyEventArgs"/>.
        /// </summary>
        /// <param name="fullUrl">The full request URL.</param>
        /// <param name="contentType">The estimated response content type based on uri parsing.</param>
        public HybridWebViewProxyEventArgs(string fullUrl, string contentType)
        {
            Url = fullUrl;
            ResponseContentType = contentType;
            QueryParams = QueryStringHelper.GetKeyValuePairs(fullUrl);
        }

        /// <summary>
        /// The full request URL.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Query string values extracted from the request URL.
        /// </summary>
        public IDictionary<string, string> QueryParams { get; }

        /// <summary>
        /// The response content type.
        /// </summary>

        public string? ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// The response stream to be used to respond to the request.
        /// </summary>
        public Stream? ResponseStream { get; set; } = null;

        /// <summary>
        /// Additional headers to be added to the response.
        /// Useful for things like adding a cache control header.
        /// </summary>
        public IDictionary<string, string>? CustomResponseHeaders { get; set; }
    }
}
