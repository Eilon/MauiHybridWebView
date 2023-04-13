# .NET MAUI HybridWebView experiment

This repo has the experimental .NET MAUI HybridWebView control, which enables hosting arbitrary HTML/JS/CSS content in a WebView and enables communication between the code _in_ the WebView (JavaScript) and the code that hosts the WebView (C#/.NET). For example, if you have an existing React JS application, you could host it in a cross-platform .NET MAUI native application, and build the back-end of the application using C# and .NET.

Example usage of the control:

```xaml
<hwv:HybridWebView
    HybridAssetRoot="hybrid_root"
    MainFile="hybrid_app.html"
    RawMessageReceived="OnHybridWebViewRawMessageReceived" />
```

And here's how .NET code can call a JavaScript method:

```c#
var sum = await myHybridWebView.InvokeJsMethodAsync<int>("JsAddNumbers", 123, 456);
```

And the reverse, JavaScript code calling a .NET method:

```js
HybridWebView.SendInvokeMessageToDotNet("CallMeFromScript", ["msg from js", 987]);
```

In addition to method invocation, sending "raw" messages is also supported.

## What's in this repo?

Projects in this repo:

* [HybridWebView](HybridWebView) --> a cross-platform .NET MAUI control that can load static web assets (HTML, JS, CSS, etc.) and send messages between JavaScript and .NET
* [MauiCSharpInteropWebView](MauiCSharpInteropWebView) --> a sample app that shows the basic functionality of sending messages and calling methods between JavaScript and .NET
* [MauiReactJSHybridApp](MauiReactJSHybridApp) --> a sample app that incorporates a pre-existing React-based todo application into a .NET MAUI cross-platform application

## Discussions/questions/comments

See the main discussion topic here: https://github.com/dotnet/maui/discussions/12009

Or please log an issue in this repo for any other topics.

## Getting Started

To get started, you'll need a .NET MAUI 7 project, then add the HybridWebView control, and add some web content to it.

Note: If you'd like to check out an already completed sample, go to https://github.com/Eilon/SampleMauiHybridWebViewProject

1. Ensure you have Visual Studio 2022 with the .NET MAUI workload installed
1. Create a **.NET MAUI App** project that targets .NET 7 (or use an existing one)
1. Add a reference to the `EJL.MauiHybridWebView` package:
    1. Right-click on the **Dependencies** node in Solution Explorer and select **Manage NuGet Packages**
    1. Select the **Browse** tab
    1. Ensure the **Include prerelease** checkbox is checked
    1. In the search box enter `ejl.mauihybridwebview`
    1. Select the matching package, and click the **Install** button
1. Register and add the `HybridWebView` control to a page in your app:
    1. In `MauiProgram.cs` add the line `builder.Services.AddHybridWebView();` to register the `HybridWebView` components
    1. Open the `MainPage.xaml` file
    1. Delete the `<ScrollView>` control and all of its contents
    1. Add a `xmlns:ejl="clr-namespace:HybridWebView;assembly=HybridWebView"` declaration to the top-level `<ContentPage ....>` tag
    1. Add the markup `<ejl:HybridWebView HybridAssetRoot="hybrid_root" MainFile="index.html" RawMessageReceived="OnHybridWebViewRawMessageReceived" />` inside the `<ContentPage>` tag
    1. Open the `MainPage.xaml.cs` file
    1. Delete the `count` field, and the `OnCounterClicked` method, and replace it with the following code:
        ```c#
        private async void OnHybridWebViewRawMessageReceived(object sender, HybridWebView.HybridWebViewRawMessageReceivedEventArgs e)
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DisplayAlert("JavaScript message", e.Message, "OK");
            });
        }
        ```
1. Now add some web content to the app:
    1. In **Solution Explorer** expand the **Resources** / **Raw** folder
    1. Create a new sub-folder called `hybrid_root`
    1. In the `hybrid_root` folder add a new file called `index.html` and replace its contents with:
        ```html
        <!DOCTYPE html>

        <html lang="en" xmlns="http://www.w3.org/1999/xhtml">
        <head>
            <meta charset="utf-8" />
            <title></title>
            <script src="_hwv/HybridWebView.js"></script>
            <script src="myapp.js"></script>
        </head>
        <body>
            <div>
                Your message: <input type="text" id="messageInput" />
            </div>
            <div>
                <button onclick="SendMessageToCSharp()">Send to C#!</button>
            </div>
        </body>
        </html>
        ```
    1. In the same folder add another file `myapp.js` with the following contents:
        ```js
        function SendMessageToCSharp() {
            var message = document.getElementById('messageInput').value;
            HybridWebView.SendRawMessageToDotNet(message);
        }
        ```
1. You can now run your .NET MAUI app with the HybridWebView control!
    1. You can run it on Windows, Android, iOS, or macOS
    1. When you launch the app, type text into the textbox and click the button to receive the message in C# code


## How to run the source code in this repo

To run this app you need to have [Visual Studio for Windows or Mac, including the .NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation?view=net-maui-7.0). Then clone this repo, open the solution, and run one of the sample projects.

## MauiReactJSHybridApp React JS app

The MauiReactJSHybridApp sample contains portions of a pre-existing Todo App built using React JS.

The original React JS Todo app sample used here is based on this sample: https://github.com/mdn/todo-react. I created a fork at https://github.com/Eilon/todo-react that incorporates some small changes to call the .NET API from JavaScript to synchronize the Todo list between the two parts of the app.

To make changes to the fork and update the .NET MAUI app, here's what I do:

1. Clone of the forked repo and open a terminal/console window in that folder
1. Run `yarn` to ensure the JavaScript dependencies are installed
1. Run `set PUBLIC_URL=/` to establish the root of the app as `/` because that's the root of the .NET MAUI HybridWebView app
1. Run `npm run build` to compile the app and produce a static version of it
   * If you get this error: `Error: error:0308010C:digital envelope routines::unsupported`
   * Then run `set NODE_OPTIONS=--openssl-legacy-provider`
   * And run this again: `npm run build`
1. This will build the HTML/JS/CSS output into a new `./build` folder
1. Go to the `MauiReactJSHybridApp`'s `Resources/Raw/ReactTodoApp` folder and delete all the existing files, and replace with the files from the previous step's `./build` folder
1. Then run the MauiReactJSHybridApp from Visual Studio

---

This project is licensed under the [MIT License](LICENSE). However, portions of the incorporated source code may be subject to other licenses:

* The `MauiReactJSHybridApp/Resources/Raw/ReactTodoApp` folder is the output of a build from https://github.com/Eilon/todo-react, which is a modified fork of https://github.com/mdn/todo-react, which is licensed under the [Mozilla Public License 2.0](https://github.com/mdn/todo-react/blob/main/LICENSE).
