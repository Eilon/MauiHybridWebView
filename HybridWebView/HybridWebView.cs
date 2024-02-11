using System.Text.Json;

namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        /// <summary>
        /// Specifies the file within the <see cref="HybridAssetRoot"/> that should be served as the main file. The
        /// default value is <c>index.html</c>.
        /// </summary>
        public string? MainFile { get; set; } = "index.html";

        /// <summary>
        /// Gets or sets the path for initial navigation after the content is finished loading. The default value is <c>/</c>.
        /// </summary>
        public string StartPath { get; set; } = "/";

        /// <summary>
        ///  The path within the app's "Raw" asset resources that contain the web app's contents. For example, if the
        ///  files are located in "ProjectFolder/Resources/Raw/hybrid_root", then set this property to "hybrid_root".
        /// </summary>
        public string? HybridAssetRoot { get; set; }

        /// <summary>
        /// The target object for JavaScript method invocations. When an "invoke" message is sent from JavaScript,
        /// the invoked method will be located on this object, and any specified parameters will be passed in.
        /// </summary>
        public object? JSInvokeTarget { get; set; }

        /// <summary>
        /// Enables web developers tools (such as "F12 web dev tools inspectors")
        /// </summary>
        public bool EnableWebDevTools { get; set; }

        public event EventHandler<HybridWebViewRawMessageReceivedEventArgs>? RawMessageReceived;

        public void Navigate(string url)
        {
            NavigateCore(url);
        }

        protected override async void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            await InitializeHybridWebView();

            Navigate(StartPath);
        }

        private partial Task InitializeHybridWebView();

        private partial void NavigateCore(string url);


#if !ANDROID && !IOS && !MACCATALYST && !WINDOWS
        private partial Task InitializeHybridWebView() => throw null!;

        private partial void NavigateCore(string url) => throw null!;
#endif

        /// <summary>
        /// Invokes a JavaScript method named <paramref name="methodName"/> and optionally passes in the parameter values specified
        /// by <paramref name="paramValues"/> by JSON-encoding each one.
        /// </summary>
        /// <param name="methodName">The name of the JavaScript method to invoke.</param>
        /// <param name="paramValues">Optional array of objects to be passed to the JavaScript method by JSON-encoding each one.</param>
        /// <returns>A string containing the return value of the called method.</returns>
        public async Task<string> InvokeJsMethodAsync(string methodName, params object[] paramValues)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException($"The method name cannot be null or empty.", nameof(methodName));
            }

            return await EvaluateJavaScriptAsync($"{methodName}({(paramValues == null ? string.Empty : string.Join(", ", paramValues.Select(v => JsonSerializer.Serialize(v))))})");
        }

        /// <summary>
        /// Invokes a JavaScript method named <paramref name="methodName"/> and optionally passes in the parameter values specified
        /// by <paramref name="paramValues"/> by JSON-encoding each one.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the return value to deserialize from JSON.</typeparam>
        /// <param name="methodName">The name of the JavaScript method to invoke.</param>
        /// <param name="paramValues">Optional array of objects to be passed to the JavaScript method by JSON-encoding each one.</param>
        /// <returns>An object of type <typeparamref name="TReturnType"/> containing the return value of the called method.</returns>
        public async Task<TReturnType?> InvokeJsMethodAsync<TReturnType>(string methodName, params object[] paramValues)
        {
            var stringResult = await InvokeJsMethodAsync(methodName, paramValues);

            if (stringResult is null)
            {
                return default;
            }
            return JsonSerializer.Deserialize<TReturnType>(stringResult);
        }

        public virtual void OnMessageReceived(string message)
        {
            var messageData = JsonSerializer.Deserialize<WebMessageData>(message);
            switch (messageData?.MessageType)
            {
                case 0: // "raw" message (just a string)
                    RawMessageReceived?.Invoke(this, new HybridWebViewRawMessageReceivedEventArgs(messageData.MessageContent));
                    break;
                case 1: // "invoke" message
                    if (messageData.MessageContent == null)
                    {
                        throw new InvalidOperationException($"Expected invoke message to contain MessageContent, but it was null.");
                    }
                    var invokeData = JsonSerializer.Deserialize<JSInvokeMethodData>(messageData.MessageContent)!;
                    InvokeDotNetMethod(invokeData);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type: {messageData?.MessageType}. Message contents: {messageData?.MessageContent}");
            }

        }

        private void InvokeDotNetMethod(JSInvokeMethodData invokeData)
        {
            if (JSInvokeTarget is null)
            {
                throw new NotImplementedException($"The {nameof(JSInvokeTarget)} property must have a value in order to invoke a .NET method from JavaScript.");
            }

            var invokeMethod = JSInvokeTarget.GetType().GetMethod(invokeData.MethodName!, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod);
            if (invokeMethod == null)
            {
                throw new InvalidOperationException($"The method {invokeData.MethodName} couldn't be found on the {nameof(JSInvokeTarget)} of type {JSInvokeTarget.GetType().FullName}.");
            }

            if (invokeData.ParamValues != null && invokeMethod.GetParameters().Length != invokeData.ParamValues.Length)
            {
                throw new InvalidOperationException($"The number of parameters on {nameof(JSInvokeTarget)}'s method {invokeData.MethodName} ({invokeMethod.GetParameters().Length}) doesn't match the number of values passed from JavaScript code ({invokeData.ParamValues.Length}).");
            }

            var paramObjectValues =
                invokeData.ParamValues?
                    .Zip(invokeMethod.GetParameters(), (s, p) => JsonSerializer.Deserialize(s, p.ParameterType))
                    .ToArray();

            var returnValue = invokeMethod.Invoke(JSInvokeTarget, paramObjectValues);
        }

        private sealed class JSInvokeMethodData
        {
            public string? MethodName { get; set; }
            public string[]? ParamValues { get; set; }
        }

        private sealed class WebMessageData
        {
            public int MessageType { get; set; }
            public string? MessageContent { get; set; }
        }

        internal static async Task<string?> GetAssetContentAsync(string assetPath)
        {
            using var stream = await GetAssetStreamAsync(assetPath);
            if (stream == null)
            {
                return null;
            }
            using var reader = new StreamReader(stream);

            var contents = reader.ReadToEnd();

            return contents;
        }

        internal static async Task<Stream?> GetAssetStreamAsync(string assetPath)
        {
            if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            {
                return null;
            }
            return await FileSystem.OpenAppPackageFileAsync(assetPath);
        }
    }
}
