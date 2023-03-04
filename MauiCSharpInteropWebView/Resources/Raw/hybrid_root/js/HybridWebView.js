// Standard methods for HybridWebView

class HybridWebViewDotNetHost {
    // TODO: Create a psudo private constructor to allow for only
    // a single instance of HybridWebViewDotNetHost
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Classes/Private_class_fields#simulating_private_constructors
    static Current = new HybridWebViewDotNetHost();
    #nextCallbackId = 1;
    #callbackMap = new Map();

    // Methods
    SendRawMessageToDotNet(message) {
        this.SendMessageToDotNet(0, message);
    }

    SendInvokeMessageToDotNet(className, methodName, paramValues = []) {
        if (paramValues && !Array.isArray(paramValues)) {
            paramValues = Array.of(paramValues);
        }

        let params = paramValues.map(x => JSON.stringify(x));

        let callback = this.CreateCallback();

        this.SendMessageToDotNet(1, JSON.stringify({ "ClassName": className, "MethodName": methodName, "CallbackId": callback.id, "ParamValues": params }));

        return callback.promise;
    }

    SendMessageToDotNet(messageType, messageContent) {
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

    CreateProxy(className) {
        const self = this;
        const proxy = new Proxy({}, {
            get: function (target, methodName) {
                return function (...args) {
                    return self.SendInvokeMessageToDotNet(className, methodName, args);
                }
            }
        });

        return proxy;
    }

    ResolveCallback(id, message) {
        const callback = this.#callbackMap.get(id);

        if (callback) {
            const obj = JSON.parse(message);
            callback.resolve(obj);
        }        
    }

    RejectCallback(id, message) {
        const callback = this.#callbackMap.get(id);

        if (callback) {
            callback.resolve(message);
        }
    }

    CreateCallback() {
        let callback = {};

        callback.id = this.#nextCallbackId++;
        callback.promise = new Promise((resolve, reject) => {
            callback.resolve = resolve;
            callback.reject = reject;
        });;

        this.#callbackMap.set(callback.id, callback);

        return callback;
    }
}

// Default instance, allow users to set the instance on their own
window.HybridWebView = HybridWebViewDotNetHost.Current;