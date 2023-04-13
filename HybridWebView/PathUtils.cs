namespace HybridWebView
{
    internal static class PathUtils
    {
        public static string NormalizePath(string filename) =>
            filename
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
    }
}
