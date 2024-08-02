namespace HybridWebView
{
    internal static class QueryStringHelper
    {
        public static string RemovePossibleQueryString(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            var indexOfQueryString = url.IndexOf('?', StringComparison.Ordinal);
            return (indexOfQueryString == -1)
                ? url
                : url.Substring(0, indexOfQueryString);
        }

        // TODO: Replace this

        /// <summary>
        /// A simple utility that takes a URL, extracts the query string and returns a dictionary of key-value pairs.
        /// Note that values are unescaped. Manually created URLs in JavaScript should use encodeURIComponent to escape values.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetKeyValuePairs(string? url)
        {
            var result = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(url))
            {
                string query = url.Substring(url.IndexOf("?") + 1);
                if (!string.IsNullOrWhiteSpace(query))
                {
                    result = query
                        .Split('&')
                        .Select(p => p.Split('='))
                        .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
                }
            }

            return result;
        }
    }
}
