// Standard methods for HybridWebView

window.HybridWebView = {
    "SendRawMessageToDotNet": function SendRawMessageToDotNet(message) {
        window.HybridWebView.SendMessageToDotNet(0, message);
    },

    "SendInvokeMessageToDotNet": function SendInvokeMessageToDotNet(methodName, paramValues) {
        if (typeof paramValues !== 'undefined') {
            if (!Array.isArray(paramValues)) {
                paramValues = [paramValues];
            }
            for (var i = 0; i < paramValues.length; i++) {
                paramValues[i] = JSON.stringify(paramValues[i]);
            }
        }

        window.HybridWebView.SendMessageToDotNet(1, JSON.stringify({ "MethodName": methodName, "ParamValues": paramValues }));
    },

    "SendMessageToDotNet": function SendMessageToDotNet(messageType, messageContent) {
        var message = JSON.stringify({ "MessageType": messageType, "MessageContent": messageContent });

        if (window.chrome && window.chrome.webview) {
            // Windows WebView2
            window.chrome.webview.postMessage(message);
        }
        else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.webwindowinterop)
        {
            // iOS and MacCatalyst WKWebView
            window.webkit.messageHandlers.webwindowinterop.postMessage(message);
        }
        else {
            // Android WebView
            hybridWebViewHost.sendMessage(message);
        }
    }
};
