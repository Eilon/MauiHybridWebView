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

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);

        _ = webView.EvaluateJavaScriptAsync($"SendToJs('hi from .net, counter={count}!')");
    }

    private void webView_MessageReceived(object sender, HybridWebView.HybridWebViewMessageReceivedEventArgs e)
    {
        Debug.WriteLine($"Web Message Received: {e.Message}");
    }
}
