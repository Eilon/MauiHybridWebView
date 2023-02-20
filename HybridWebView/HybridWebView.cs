using System.Reflection;
using System.Text.Json;

namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        public string MainFile { get; set; }

        /// <summary>
        ///  The path within the app's "Raw" asset resources that contain the web app's contents. For example, if the
        ///  files are located in "ProjectFolder/Resources/Raw/hybrid_root", then set this property to "hybrid_root".
        /// </summary>
        public string HybridAssetRoot { get; set; }

        /// <summary>
        /// The target object for JavaScript method invocations. When an "invoke" message is sent from JavaScript,
        /// the invoked method will be located on this object, and any specified parameters will be passed in.
        /// </summary>
        public object JSInvokeTarget { get; set; }

        public event EventHandler<HybridWebViewRawMessageReceivedEventArgs> RawMessageReceived;

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            InitializeHybridWebView();
        }

        partial void InitializeHybridWebView();

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
        public async Task<TReturnType> InvokeJsMethodAsync<TReturnType>(string methodName, params object[] paramValues)
        {
            var stringResult = await InvokeJsMethodAsync(methodName, paramValues);

            return JsonSerializer.Deserialize<TReturnType>(stringResult);
        }

        public virtual void OnMessageReceived(string message)
        {
            var messageData = JsonSerializer.Deserialize<WebMessageData>(message);
            switch (messageData.MessageType)
            {
                case 0: // "raw" message (just a string)
                    RawMessageReceived?.Invoke(this, new HybridWebViewRawMessageReceivedEventArgs(messageData.MessageContent));
                    break;
                case 1: // "invoke" message
                    var invokeData = JsonSerializer.Deserialize<JSInvokeMethodData>(messageData.MessageContent);
                    InvokeDotNetMethod(invokeData);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type: {messageData.MessageType}. Message contents: {messageData.MessageContent}");
            }

        }

        private void InvokeDotNetMethod(JSInvokeMethodData invokeData)
        {
            if (JSInvokeTarget is null)
            {
                throw new NotImplementedException($"The {nameof(JSInvokeTarget)} property must have a value in order to invoke a .NET method from JavaScript.");
            }

            object callingClass = JSInvokeTarget;
            MethodInfo invokeMethod = JSInvokeTarget.GetType().GetMethod(invokeData.MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.InvokeMethod);
                
            if(invokeMethod is null)
            {
                var test = LocalRegisteredCallbacks.Where(x => x.Key.Equals(invokeData.MethodName)).Select(x => x.Value).FirstOrDefault();
                invokeMethod = test.Item2;
                callingClass = test.Item1;

            }

            if (invokeData.ParamValues != null && invokeMethod.GetParameters().Length != invokeData.ParamValues.Length)
            {
                throw new InvalidOperationException($"The number of parameters on {nameof(JSInvokeTarget)}'s method {invokeData.MethodName} ({invokeMethod.GetParameters().Length}) doesn't match the number of values passed from JavaScript code ({invokeData.ParamValues.Length}).");
            }

            var paramObjectValues =
                invokeData.ParamValues?
                    .Zip(invokeMethod.GetParameters(), (s, p) => JsonSerializer.Deserialize(s, p.ParameterType))
                    .ToArray();

            var returnValue = invokeMethod.Invoke(callingClass, paramObjectValues);
        }


        internal readonly Dictionary<string, (object,MethodInfo)> LocalRegisteredCallbacks = new();
        public void AddLocalCallback(object callingClass, MethodInfo action)
        {
            if (LocalRegisteredCallbacks.ContainsKey(action.Name))
                LocalRegisteredCallbacks.Remove(action.Name);

            LocalRegisteredCallbacks.Add(action.Name, (callingClass, action));
        }
        private sealed class JSInvokeMethodData
        {
            public string MethodName { get; set; }
            public string[] ParamValues { get; set; }
        }

        private sealed class WebMessageData
        {
            public int MessageType { get; set; }
            public string MessageContent { get; set; }
        }

        internal static async Task<string> GetAssetContentAsync(string assetPath)
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

        internal static async Task<Stream> GetAssetStreamAsync(string assetPath)
        {
            if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            {
                return null;
            }
            return await FileSystem.OpenAppPackageFileAsync(assetPath);
        }
    }
}
