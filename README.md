# .NET MAUI HybridWebView experiment

Projects in this repo:

* HybridWebView --> a cross-platform .NET MAUI control that can load static web assets (HTML, JS, CSS, etc.) and send messages between JavaScript and .NET
* MauiCSharpInteropWebView --> a sample app that shows the basic functionality
* MauiReactJSHybridApp --> a sample app that incorporates a pre-existing React-based todo application

See discussion topic here: https://github.com/dotnet/maui/discussions/12009

---

This project is licensed under the [MIT License](LICENSE). However, portions of the incorporated source code may be subject to other licenses:

* The `MauiReactJSHybridApp/Resources/Raw/ReactTodoApp` folder is the output of a build from https://github.com/Eilon/todo-react, which is a modified fork of https://github.com/mdn/todo-react, which is licensed under the [Mozilla Public License 2.0](https://github.com/mdn/todo-react/blob/main/LICENSE).
