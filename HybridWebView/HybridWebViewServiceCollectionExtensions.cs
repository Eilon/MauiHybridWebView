using HybridWebView;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HybridWebViewServiceCollectionExtensions
    {
		public static void AddHybridWebView(this IServiceCollection services)
		{
			services.ConfigureMauiHandlers(static handlers => handlers.AddHandler<HybridWebView.HybridWebView, HybridWebViewHandler>());
		}
	}
}
