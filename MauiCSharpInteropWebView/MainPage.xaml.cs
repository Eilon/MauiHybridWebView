using System;
using System.Diagnostics;

namespace MauiCSharpInteropWebView;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();

        webView.JSInvokeTarget = new MyJSInvokeTarget(this);
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
            Debug.WriteLine($"I'm a .NET method called from JavaScript with message='{message}' and value={value}");
        }
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);

        _ = await webView.EvaluateJavaScriptAsync($"SendToJs('hi from .net, counter={count}!')");
        _ = await webView.InvokeJsMethodAsync("SendToJsWithArgs", 123.456789, "Nice!", DateTimeOffset.Now);
        var sum = await webView.InvokeJsMethodAsync<int>("JsAddNumbers", 123, 456);
        Debug.WriteLine($"JS Return value received with sum: {sum}");
    }

    private void webView_MessageReceived(object sender, HybridWebView.HybridWebViewRawMessageReceivedEventArgs e)
    {
        Debug.WriteLine($"Web Message Received: {e.Message}");
    }
}
