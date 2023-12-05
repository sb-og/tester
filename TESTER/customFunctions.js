// customFunctions.js
function callGetDatabaseAddress() {
    // Wywo³aj funkcjê getDatabaseAddress na stronie
    var result = getDatabaseAddress();

    // Przekazanie wyniku do kodu C# (przy u¿yciu CefSharp)
    CefSharp.PostMessage({
        name: "SetOutputFromJavascript",
        result: result
    });
}