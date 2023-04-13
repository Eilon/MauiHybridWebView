using System.Reflection;

namespace HybridWebView
{
    internal static class KnownStaticFileProvider
    {
        private static readonly string[] KnownStaticFilesNames = { "HybridWebView.js" };
        private static readonly Assembly ThisAssembly = typeof(KnownStaticFileProvider).Assembly;
        private static readonly string KnownStaticFilePrefix = "_hwv" + Path.DirectorySeparatorChar;

        public static Stream? GetKnownResourceStream(string resourceRelativeUrl)
        {
            resourceRelativeUrl = PathUtils.NormalizePath(resourceRelativeUrl);

            if (!resourceRelativeUrl.StartsWith(KnownStaticFilePrefix, StringComparison.Ordinal))
            {
                return null;
            }
            var resourceName = resourceRelativeUrl.Substring(KnownStaticFilePrefix.Length);

            if (!KnownStaticFilesNames.Contains(resourceName))
            {
                return null;
            }

            return ThisAssembly.GetManifestResourceStream("HybridWebView.KnownStaticFiles." + resourceName);
        }
    }
}
