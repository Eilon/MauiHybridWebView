namespace HybridWebView
{
    public class HybridWebViewMessageReceivedEventArgs : EventArgs
    {
        public HybridWebViewMessageReceivedEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
