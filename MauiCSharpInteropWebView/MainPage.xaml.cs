namespace MauiCSharpInteropWebView;

public partial class MainPage : ContentPage
{
    private HybridAppPageID _currentPage;
    private int _messageCount;

    public MainPage()
    {
        InitializeComponent();

        BindingContext = this;

        myHybridWebView.JSInvokeTarget = new MyJSInvokeTarget(this);

        // Register in the constructor or anywhere else
        myHybridWebView.AddLocalCallback(this, nameof(AddLocalCallBackTest));
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

    private async void AddLocalCallBackTest(string message, int value)
    {
        WriteToLog($"I'm a .NET method called from JavaScript with message='{message}' and value={value}, using a local registration");
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
        ManualRegister = 3,
    }
}
