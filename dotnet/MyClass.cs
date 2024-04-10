using System.Runtime.InteropServices.JavaScript;

namespace dotnet_wasm_sample;

public partial class MyClass
{
    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();

    [JSExport]
    internal static string Greeting()
    {
        var text = $"Hello, World! Greetings from {GetHRef()}";
        Console.WriteLine(text);

        return text;
    }
}