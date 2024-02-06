using System.IO.Compression;
using System.Text;

namespace MauiCSharpInteropWebView;

public partial class MainPage : ContentPage
{
    private HybridAppPageID _currentPage;
    private int _messageCount;

    public MainPage()
    {
        InitializeComponent();

        BindingContext = this;

#if DEBUG
        myHybridWebView.EnableWebDevTools = true;
#endif

        myHybridWebView.JSInvokeTarget = new MyJSInvokeTarget(this);

        myHybridWebView.OnProxyRequest = MyHybridWebView_OnProxyRequest;
    }

    private async Task MyHybridWebView_OnProxyRequest(HybridWebView.HybridWebViewRestEventArgs e)
    { 
        //In an app, you might load responses from a sqlite database, zip file, or create files in memory (e.g. using SkiaSharp or System.Drawing)

        //Check to see if our custom parameter is present.
        if (e.QueryParams.ContainsKey("operation"))
        {
            //Android appears to make all query string values lowercase, so we need to check for both cases to be safe.
            switch (e.QueryParams["operation"])
            {
                case "loadimagefromzip":
                case "loadImageFromZip":
                    string fileName = null;

                    //Try to get the fileName parameter in both cases.
                    if (e.QueryParams.ContainsKey("fileName"))
                    {
                        fileName = e.QueryParams["fileName"];
                    } 
                    else if (e.QueryParams.ContainsKey("filename"))
                    {
                        fileName = e.QueryParams["filename"];
                    }

                    //Ensure the file name parameter is present.
                    if (fileName != null)
                    {
                        //Load local zip file. 
                        using (var stream = await FileSystem.OpenAppPackageFileAsync("media/pictures.zip"))
                        {
                            //Unzip file and check to see if it has the requested file name.
                            using (var archive = new ZipArchive(stream))
                            {
                                var file = archive.Entries.Where(x => x.FullName == fileName).FirstOrDefault();

                                if (file != null)
                                {
                                    //Copy the file stream to a memory stream.
                                    var ms = new MemoryStream();
                                    using (var fs = file.Open())
                                    {
                                        await fs.CopyToAsync(ms);
                                    }

                                    //Rewind stream.
                                    ms.Position = 0;

                                    e.ResponseStream = ms;
                                    e.ContentType = "image/jpeg";
                                }
                            }
                        }
                    }
                    break;
                case "loadImageFromWeb":
                case "loadimagefromweb":
                    int tileId = -1;

                    //Try to get the tileId parameter in both cases.
                    if (e.QueryParams.ContainsKey("tileId"))
                    {
                        tileId = int.Parse(e.QueryParams["tileId"]);
                    } else if (e.QueryParams.ContainsKey("tileid"))
                    {
                        tileId = int.Parse(e.QueryParams["tileid"]);
                    }

                    //Ensure the tileId parameter is present.
                    if (tileId != -1)
                    {
                        //Apply custom logic. In this case convert into a quadkey value for Bing Maps.
                        var quadKey = new StringBuilder();
                        for (var i = tileId; i > 0; i /= 4)
                        {
                            quadKey.Insert(0, tileId % 4);
                        }

                        //Create URL using the tileId parameter. 
                        var url = $"https://ecn.t0.tiles.virtualearth.net/tiles/a{quadKey.ToString()}.jpeg?g=14245";

#if WINDOWS
                        var client = new HttpClient();
#elif ANDROID
                        var client = new HttpClient(new Xamarin.Android.Net.AndroidMessageHandler());
#endif

                        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

                        //Copy the response stream to a memory stream.
                        var ms2 = new MemoryStream();
                        response.Content.CopyToAsync(ms2).Wait();
                        ms2.Position = 0;
                        e.ResponseStream = ms2;
                        e.ContentType = "image/jpeg";
                    }
                    break;
            }
        }
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

    private void WriteToLog(string message)
    {
        MessageLog += Environment.NewLine + $"{_messageCount++}: " + message;
        MessageLogPosition = MessageLog.Length;
        OnPropertyChanged(nameof(MessageLog));
        OnPropertyChanged(nameof(MessageLogPosition));
    }

    private sealed class MyJSInvokeTarget
    {
        private MainPage _mainPage;

        public MyJSInvokeTarget(MainPage mainPage)
        {
            _mainPage = mainPage;
        }

        public void CallMeFromScript(string message, int value)
        {
            _mainPage.WriteToLog($"I'm a .NET method called from JavaScript with message='{message}' and value={value}");
        }
    }

    private enum HybridAppPageID
    {
        MainPage = 0,
        RawMessages = 1,
        MethodInvoke = 2,
    }
}
