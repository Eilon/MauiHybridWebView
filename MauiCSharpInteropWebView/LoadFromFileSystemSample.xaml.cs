using Microsoft.Maui.Storage;
using SQLite;

namespace MauiCSharpInteropWebView;

public partial class LoadFromFileSystemSample : ContentPage
{
	public LoadFromFileSystemSample()
	{
        //For this sample we need a HTML file in the file system. 
        //We will copy the file from the app package Raw folder to the app data storage. If it isn't already there.

        LoadFileIntoFileSystem("SampleFileSystemPage.html");
        LoadFileIntoFileSystem("SampleFileSystemImage.png");

        InitializeComponent();

        //Set the target for JavaScript interop.
        myHybridWebView.JSInvokeTarget = new MyJSInvokeTarget();

        //Monitor proxy requests.
        myHybridWebView.ProxyRequestReceived += WebView_ProxyRequestReceived;
    }

    #region Private Methods

    private async void LoadFileIntoFileSystem(string assetPath)
    {
        //Get local file path to app data storage.
        var localPath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(assetPath));

        //Check to see if the file exists in the app data storage already.
        if (!File.Exists(localPath))
        {
            //If it doesn't, assume it is an asset and copy the file to local app data storage access Raw folder.
            using (var asset = await FileSystem.OpenAppPackageFileAsync(assetPath))
            {
                using (var file = File.Create(localPath))
                {
                    asset.CopyTo(file);
                }
            }
        }
    }

    private async Task WebView_ProxyRequestReceived(HybridWebView.HybridWebViewProxyEventArgs args)
    {
        // Check to see if our custom parameter is present.
        if (args.QueryParams.ContainsKey("operation"))
        {
            switch (args.QueryParams["operation"])
            {
                case "loadFromFileSystem":
                    if (args.QueryParams.TryGetValue("fileName", out string fileName) && !string.IsNullOrWhiteSpace(fileName))
                    {
                        var filePath = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, Path.GetFileName(fileName));

                        //Check to see if the file exists.
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                using (var fs = System.IO.File.OpenRead(filePath))
                                {
                                    var ms = new MemoryStream();
                                    await fs.CopyToAsync(ms);
                                    ms.Position = 0;

                                    args.ResponseStream = ms;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Write(ex.Message);
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

    private void OnHybridWebViewRawMessageReceived(object sender, HybridWebView.HybridWebViewRawMessageReceivedEventArgs e)
    {

    }
}