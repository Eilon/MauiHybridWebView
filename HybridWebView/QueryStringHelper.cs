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

        public static Dictionary<string, string> GetKeyValuePairs(string? url)
        {
            var result = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(url))
            {
                var query = new Uri(url).Query;
                if (query != null && query.Length > 1)
                {
                    result = query
                        .Substring(1)
                        .Split('&')
                        .Select(p => p.Split('='))
                        .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
                }
            }

            return result;
        }
    }
}
