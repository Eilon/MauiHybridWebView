// Standard methods for HybridWebView

window.HybridWebView = {
    /**
     * Sends a message to .NET using the built in
     * @param {string} message Message to send.
     */
    "SendRawMessageToDotNet": function SendRawMessageToDotNet(message) {
        window.HybridWebView.SendMessageToDotNet(0, message);
    },

    /**
     * Invoke a .NET method. No result is expected.
     * @param {string} methodName Name of .NET method to invoke.
     * @param {any[]} paramValues Parameters to pass to the method.
     */
    "SendInvokeMessageToDotNet": function SendInvokeMessageToDotNet(methodName, paramValues) {
        if (typeof paramValues !== 'undefined') {
            if (!Array.isArray(paramValues)) {
                paramValues = [paramValues];
            }
            for (var i = 0; i < paramValues.length; i++) {
                // Let 'null' and 'undefined' be passed as-is, but stringify all other values
                if (paramValues[i] != null) {
                    paramValues[i] = JSON.stringify(paramValues[i]);
                }
            }
        }

        window.HybridWebView.SendMessageToDotNet(1, JSON.stringify({ "MethodName": methodName, "ParamValues": paramValues }));
    },

    /**
     * Asynchronously invoke .NET method and get a result. 
     * Leverages the proxy to send the message to .NET.
     * @param {string} methodName Name of .NET method to invoke.
     * @param {any[]} paramValues Parameters to pass to the method.
     * @returns {Promise<any>} Result of the .NET method.
     */
    "SendInvokeMessageToDotNetAsync": async function SendInvokeMessageToDotNetAsync(methodName, paramValues) {
        const body = {
            MethodName: methodName
        };

        if (typeof paramValues !== 'undefined') {
            if (!Array.isArray(paramValues)) {
                paramValues = [paramValues];
            }

            for (var i = 0; i < paramValues.length; i++) {
                paramValues[i] = JSON.stringify(paramValues[i]);
            }

            if (paramValues.length > 0) {
                body.ParamValues = paramValues;
            }
        }

        const message = JSON.stringify(body);

        try {
            // Android web view doesn't support getting the body of a POST request, so we use a GET request instead and pass the body as a query string parameter.
            var requestUrl = `${window.location.origin}/proxy?__ajax=${encodeURIComponent(message)}`;

            const rawResponse = await fetch(requestUrl, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });
            const response = await rawResponse.json();

            if (response) {
                if (response.IsJson) {
                    return JSON.parse(response.Result);
                }

                return response.Result;
            }
        } catch (e) { }

        return null;
    },

    /**
     * Sends a message to .NET using the built in 
     * @private
     * @param {number} messageType The type of message to send.
     * @param {string} messageContent The message content.
     */
    "SendMessageToDotNet": function SendMessageToDotNet(messageType, messageContent) {
        var message = JSON.stringify({ "MessageType": messageType, "MessageContent": messageContent });

        if (window.chrome && window.chrome.webview) {
            // Windows WebView2
            window.chrome.webview.postMessage(message);
        }
        else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.webwindowinterop) {
            // iOS and MacCatalyst WKWebView
            window.webkit.messageHandlers.webwindowinterop.postMessage(message);
        }
        else {
            // Android WebView
            hybridWebViewHost.sendMessage(message);
        }
    }
};
