using System;
using System.Diagnostics;

namespace MauiCSharpInteropWebView;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
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
        var sum = await webView.InvokeJsMethodAsync<int>("SendToJsAddNumbers", 123, 456);
        Debug.WriteLine($"JS Return value received with sum: {sum}");
    }

    private void webView_MessageReceived(object sender, HybridWebView.HybridWebViewMessageReceivedEventArgs e)
    {
        Debug.WriteLine($"Web Message Received: {e.Message}");
    }
}
