using HybridWebView;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace MauiCSharpInteropWebView;

public partial class MainPage : ContentPage
{
    private HybridAppPageID _currentPage;
    private int _messageCount;

    private MBTileManager _mbTileManager;

    public MainPage()
    {
        InitializeComponent();

        BindingContext = this;

#if DEBUG
        myHybridWebView.EnableWebDevTools = true;
#endif

        //_mbTileManager = new MBTileManager("world_tiles.mbtiles");
        _mbTileManager = new MBTileManager("countries-raster.mbtiles");

        myHybridWebView.JSInvokeTarget = new MyJSInvokeTarget(this);

        myHybridWebView.ProxyRequestReceived += MyHybridWebView_OnProxyRequestReceived;

        myHybridWebView.HybridWebViewInitialized += MyHybridWebView_WebViewInitialized;
    }

    private void MyHybridWebView_WebViewInitialized(object sender, HybridWebViewInitializedEventArgs e)
    {
#if WINDOWS
        // Disable the user manually zooming
        e.WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
#endif
    }

    public string CurrentPageName => $"Current hybrid page: {_currentPage}";
    public string MessageLog { get; private set; }
    public int MessageLogPosition { get; private set; }
    public bool PageAllowsRawMessage => _currentPage == HybridAppPageID.RawMessages;
    public bool PageAllowsMethodInvoke => _currentPage == HybridAppPageID.MethodInvoke;


    private async void OnSendRawMessageToJS(object sender, EventArgs e)
    {
        _ = await myHybridWebView.EvaluateJavaScriptAsync($"SendToJs('Sent from .NET, the time is: {DateTimeOffset.Now}!')");
    }

    private async void OnInvokeJSMethod(object sender, EventArgs e)
    {
        var sum = await myHybridWebView.InvokeJsMethodAsync<int>("JsAddNumbers", 123, 456);
        WriteToLog($"JS Return value received with sum: {sum}");
    }

    private void OnHybridWebViewRawMessageReceived(object sender, HybridWebView.HybridWebViewRawMessageReceivedEventArgs e)
    {
        const string PagePrefix = "page:";
        if (e.Message.StartsWith(PagePrefix, StringComparison.Ordinal))
        {
            _currentPage = (HybridAppPageID)int.Parse(e.Message.Substring(PagePrefix.Length));
            OnPropertyChanged(nameof(CurrentPageName));
            OnPropertyChanged(nameof(PageAllowsRawMessage));
            OnPropertyChanged(nameof(PageAllowsMethodInvoke));
        }
        else
        {
            WriteToLog($"Web Message Received: {e.Message}");
        }
    }

    private async Task MyHybridWebView_OnProxyRequestReceived(HybridWebView.HybridWebViewProxyEventArgs args)
    {
        // In an app, you might load responses from a sqlite database, zip file, or create files in memory (e.g. using SkiaSharp or System.Drawing)

        // Check to see if our custom parameter is present.
        if (args.QueryParams.ContainsKey("operation"))
        {
            switch (args.QueryParams["operation"])
            {
                case "loadImageFromZip":
                    // Ensure the file name parameter is present.
                    if (args.QueryParams.TryGetValue("fileName", out string fileName) && fileName != null)
                    {
                        // Load local zip file. 
                        using var stream = await FileSystem.OpenAppPackageFileAsync("media/pictures.zip");

                        // Unzip file and check to see if it has the requested file name.
                        using var archive = new ZipArchive(stream);

                        var file = archive.Entries.Where(x => x.FullName == fileName).FirstOrDefault();

                        if (file != null)
                        {
                            // Copy the file stream to a memory stream.
                            var ms = new MemoryStream();
                            using (var fs = file.Open())
                            {
                                await fs.CopyToAsync(ms);
                            }

                            // Rewind stream.
                            ms.Position = 0;

                            args.ResponseStream = ms;
                            args.ResponseContentType = "image/jpeg";
                        }
                    }
                    break;

                case "loadImageFromWeb":
                    if (args.QueryParams.TryGetValue("tileId", out string tileIdString) && int.TryParse(tileIdString, out var tileId))
                    {
                        // Apply custom logic. In this case convert into a quadkey value for Bing Maps.
                        var quadKey = new StringBuilder();
                        for (var i = tileId; i > 0; i /= 4)
                        {
                            quadKey.Insert(0, (tileId % 4).ToString(CultureInfo.InvariantCulture));
                        }

                        //Create URL using the tileId parameter. 
                        var url = $"https://ecn.t0.tiles.virtualearth.net/tiles/a{quadKey}.jpeg?g=14245";

#if ANDROID
                        var client = new HttpClient(new Xamarin.Android.Net.AndroidMessageHandler());
#else
                        var client = new HttpClient();
#endif

                        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

                        // Copy the response stream to a memory stream.
                        var ms2 = new MemoryStream();

                        // TODO: Remove the Wait()
                        response.Content.CopyToAsync(ms2).Wait();
                        ms2.Position = 0;
                        args.ResponseStream = ms2;
                        args.ResponseContentType = "image/jpeg";
                    }
                    break;

                case "loadMapTile":
                    if (args.QueryParams.TryGetValue("x", out string xString) && long.TryParse(xString, out var x) &&
                        args.QueryParams.TryGetValue("y", out string yString) && long.TryParse(yString, out var y) &&
                        args.QueryParams.TryGetValue("z", out string zString) && long.TryParse(zString, out var z))
                    {
                        var tileBytes = _mbTileManager.GetTile(x, y, z);
                        if (tileBytes != null)
                        {
                            args.ResponseStream = new MemoryStream(tileBytes);
                            args.ResponseContentType = "image/jpeg";
                        }
                    }
                    break;

                default:
                    break;
            }
        }
    }

    private void WriteToLog(string message)
    {
        MessageLog += Environment.NewLine + $"{_messageCount++}: " + message;
        MessageLogPosition = MessageLog.Length;
        OnPropertyChanged(nameof(MessageLog));
        OnPropertyChanged(nameof(MessageLogPosition));
    }

    private sealed class MyJSInvokeTarget
    {
        private readonly MainPage _mainPage;

        public MyJSInvokeTarget(MainPage mainPage)
        {
            _mainPage = mainPage;
        }

        public void CallMeFromScript(string message, int value)
        {
            _mainPage.WriteToLog($"I'm a .NET method called from JavaScript with message='{message}' and value={value}");
        }

        public void CallEmptyParams(string thisIsNull, int thisIsUndefined)
        {
            _mainPage.WriteToLog($"I'm a .NET method called from JavaScript with null='{thisIsNull}' and undefined={thisIsUndefined}");
        }

        public void CallNoParams()
        {
            _mainPage.WriteToLog($"I'm a .NET method called from JavaScript with no params");
        }

        /// <summary>
        /// An example of a round trip method that takes an input multiple parameters and returns a simple value type (string).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string RoundTripCallFromScript(string message, int value)
        {
            _mainPage.WriteToLog($"I'm a round trip .NET method called from JavaScript with message='{message}' and value={value}");

            return $"C# says: I got your message='{message}' and value={value}";
        }

        /// <summary>
        /// An example of a round trip method that takes an input parameter and returns a simple value type (number).
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public double Fibonacci(int n)
        {
            _mainPage.WriteToLog($"I'm a round trip .NET method called from JavaScript calling Fibonacci with n={n}");

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

        /// <summary>
        /// An example of a round trip method that returns an array of object without any input parameters.
        /// </summary>
        /// <returns></returns>
        public object GetObjectResponse()
        {
            _mainPage.WriteToLog($"I'm a round trip .NET method called from JavaScript getting an object without any input parameters");

            return new List<object>()
            {
                new { Name = "John", Age = 42 },
                new { Name = "Jane", Age = 39 },
                new { Name = "Sam", Age = 13 },
            };
        }

        /// <summary>
        /// An example of a round trip method that takes an input parameter and returns an object of type class.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public KeyValuePair<string, double> GetObjectResponseWithInput(double value)
        {
            _mainPage.WriteToLog($"I'm a round trip .NET method called from JavaScript getting an object with input parameter value={value}");

            return KeyValuePair.Create("value", value);
        }
    }

    private enum HybridAppPageID
    {
        MainPage = 0,
        RawMessages = 1,
        MethodInvoke = 2,
        ProxyUrls = 3,
    }
}
