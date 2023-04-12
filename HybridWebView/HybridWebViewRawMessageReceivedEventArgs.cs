namespace HybridWebView
{
    public class HybridWebViewRawMessageReceivedEventArgs : EventArgs
    {
        public HybridWebViewRawMessageReceivedEventArgs(string? message)
        {
            Message = message;
        }

        public string? Message { get; }
    }
}
