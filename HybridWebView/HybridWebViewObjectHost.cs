using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace HybridWebView
{
    // TODO:
    // - Name converter
    public class HybridWebViewObjectHost
    {
        /// <summary>
        /// Dictionary of objects
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _hostObjects = new();

        private readonly IWebView _webView;

        internal HybridWebViewObjectHost(IWebView webView)
        {
            _webView = webView;
        }

        /// <summary>
        /// Event is raised when a method is invokved from JavaScript and 
        /// no object with the matching <see cref="HybridWebViewResolveObjectEventArgs.ObjectName"/>
        /// was found. You can call <see cref="AddObject(string, object)"/> from within this
        /// event handler to add an object.
        /// </summary>
        public event EventHandler<HybridWebViewResolveObjectEventArgs> ResolveObject;

        /// <summary>
        /// Add a object with the given name
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="obj">object</param>
        /// <returns>returns true if successfully added otherwise false if object with name already exists</returns>
        public bool AddObject(string name, object obj)
        {
            return _hostObjects.TryAdd(name, obj);
        }

        public bool RemoveObject(string name)
        {
            return _hostObjects.TryRemove(name, out _);
        }

        internal void InvokeDotNetMethod(JSInvokeMethodData invokeData)
        {
            //TODO: validate invokeData
            if(!_hostObjects.ContainsKey(invokeData.ClassName))
            {
                // Give the user an opportunity to call AddObject
                ResolveObject?.Invoke(this, new HybridWebViewResolveObjectEventArgs { ObjectName = invokeData.ClassName, Host = this });
            }

            if(!_hostObjects.TryGetValue(invokeData.ClassName, out var target))
            {
                RejectCallback(invokeData.CallbackId, $"Invalid class name {invokeData.ClassName}.");

                return;
            }

            var invokeMethod = target.GetType().GetMethod(invokeData.MethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);

            if(invokeMethod == null)
            {
                RejectCallback(invokeData.CallbackId, $"Invalid method {invokeData.ClassName}.{invokeData.MethodName}.");

                return;
            }

            try
            {
                var parameters = GetMethodParams(invokeData.ClassName, invokeData.MethodName, invokeMethod, invokeData.ParamValues);

                var returnValue = invokeMethod.Invoke(target, parameters);

                switch (returnValue)
                {
                    case ValueTask valueTask:
                        _ = ResolveTask(invokeData.CallbackId, valueTask.AsTask());
                        break;

                    case Task task:
                        _ = ResolveTask(invokeData.CallbackId, task);
                        break;

                    default:
                        ResolveCallback(invokeData.CallbackId, JsonSerializer.Serialize(returnValue));
                        break;
                }
            }
            catch (Exception ex)
            {
                RejectCallback(invokeData.CallbackId, ex.ToString());
            }
        }

        private static object[] GetMethodParams(string className, string methodName, MethodInfo invokeMethod, string[] paramValues)
        {
            var dotNetMethodParams = invokeMethod.GetParameters();

            if (dotNetMethodParams.Length == 0 && paramValues.Length == 0)
            {
                return null;
            }

            if (dotNetMethodParams.Length == 0 && paramValues.Length > 0)
            {
                throw new InvalidOperationException($"The method {className}.{methodName} takes Zero(0) parameters, was called {paramValues.Length} parameter(s).");
            }

            var hasParamArray = dotNetMethodParams.Last().GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;

            if (hasParamArray)
            {
                throw new InvalidOperationException($"The method {className}.{methodName} has a parameter array as it's last argument which is not currently supported.");
            }

            if (dotNetMethodParams.Length == paramValues.Length)
            {
                return paramValues
                    .Zip(dotNetMethodParams, (s, p) => JsonSerializer.Deserialize(s, p.ParameterType))
                    .ToArray();
            }

            var methodParams = new object[dotNetMethodParams.Length];
            var missingParams = dotNetMethodParams.Length - paramValues.Length;

            for (var i = 0; i < paramValues.Length; i++)
            {
                var paramType = dotNetMethodParams[i].ParameterType;
                var paramValue = paramValues[i];
                methodParams[i] = JsonSerializer.Deserialize(paramValue, paramType);
            }

            Array.Fill(methodParams, Type.Missing, paramValues.Length, missingParams);

            return methodParams;
        }

        private async Task ResolveTask(int callbackId, Task task)
        {
            await task;

            object result = null;

            if (task.GetType().IsGenericType)
            {
                result = task.GetType().GetProperty("Result").GetValue(task);
            }

            ResolveCallback(callbackId, JsonSerializer.Serialize(result));
        }


        private void ResolveCallback(int id, string json)
        {
            _webView.EvaluateJavaScriptAsync($"HybridWebViewDotNetHost.Current.ResolveCallback({id}, '{json}')").ContinueWith(t =>
            {
                if(t.Status == TaskStatus.Faulted)
                {
                    //TODO: Report error, add a new event maybe?
                    var ex = t.Exception;
                }                
            });
        }

        private void RejectCallback(int id, string message)
        {
            _webView.EvaluateJavaScriptAsync($"HybridWebViewDotNetHost.Current.RejectCallback({id}, '{message}')").ContinueWith(t =>
            {
                if (t.Status == TaskStatus.Faulted)
                {
                    //TODO: Report error, add a new event maybe?
                    var ex = t.Exception;
                }
            });
        }

        internal sealed class JSInvokeMethodData
        {
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public int CallbackId { get; set; }
            public string[] ParamValues { get; set; }
        }
    }
}
