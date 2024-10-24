using HybridWebView;

namespace ExampleWrappedHybridWebViewLibrary
{
    /// <summary>
    /// A custom control that wraps a hybrid web view and loads embedded resources in addition to supporting the 
    /// Hybrid asset root and all other capabilites of the HybridWebView.
    /// </summary>
    public class MyCustomControl: Grid
    {
        private readonly HybridWebView.HybridWebView _webView;

        #region Constructor

        public MyCustomControl() : base()
        {
            bool enableWebDevTools = false;

#if DEBUG
            //Enable web dev tools when in debug mode.
            enableWebDevTools = true;
#endif

            //Create a web view control.
            _webView = new HybridWebView.HybridWebView
            {
                HybridAssetRoot = HybridAssetRoot ?? "hybrid_root",
                MainFile = "proxy?operation=embeddedResource&resourceName=EmbeddedWebPageResource.html",
                EnableWebDevTools = enableWebDevTools
            };

            //Set the target for JavaScript interop.
            _webView.JSInvokeTarget = new MyJSInvokeTarget();

            //Monitor proxy requests.
            _webView.ProxyRequestReceived += WebView_ProxyRequestReceived;

#if WINDOWS
            //In Windows, disable manual user zooming of web pages. 
            _webView.HybridWebViewInitialized += (s, e) =>
            {
                //Disable the user manually zooming. Don't want the user accidentally zooming the HTML page.
                e.WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            };
#endif

            //Add the web view to the control.
            this.Children.Insert(0, _webView);
        }

        #endregion

        #region Public Properties

        public string? HybridAssetRoot { get; set; } = null;

        #endregion

        #region Private Methods

        private async Task WebView_ProxyRequestReceived(HybridWebView.HybridWebViewProxyEventArgs args)
        {
            // Check to see if our custom parameter is present.
            if (args.QueryParams.ContainsKey("operation"))
            {
                switch (args.QueryParams["operation"])
                {
                    case "embeddedResource":
                        if (args.QueryParams.TryGetValue("resourceName", out string? resourceName) && !string.IsNullOrWhiteSpace(resourceName))
                        {
                            var thisAssembly = typeof(MyCustomControl).Assembly;
                            var assemblyName = thisAssembly.GetName().Name;
                            using (var fs = thisAssembly.GetManifestResourceStream($"{assemblyName}.{resourceName.Replace("/", ".")}"))
                            {
                                if (fs != null)
                                {
                                    var ms = new MemoryStream();
                                    await fs.CopyToAsync(ms);
                                    ms.Position = 0;

                                    args.ResponseStream = ms;
                                    args.ResponseContentType = PathUtils.GetMimeType(resourceName);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private sealed class MyJSInvokeTarget
        {
            public MyJSInvokeTarget()
            {
            }

            /// <summary>
            /// An example of a round trip method that takes an input parameter and returns a simple value type (number).
            /// </summary>
            /// <param name="n"></param>
            /// <returns></returns>
            public double Fibonacci(int n)
            {
                if (n == 0) return 0;

                int prev = 0;
                int next = 1;
                for (int i = 1; i < n; i++)
                {
                    int sum = prev + next;
                    prev = next;
                    next = sum;
                }
                return next;
            }
        }

        #endregion
    }
}
