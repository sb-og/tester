// customFunctions.js
function callGetDatabaseAddress() {
    // Wywo�aj funkcj� getDatabaseAddress na stronie
    var result = getDatabaseAddress();

    // Przekazanie wyniku do kodu C# (przy u�yciu CefSharp)
    CefSharp.PostMessage({
        name: "SetOutputFromJavascript",
        result: result
    });
}