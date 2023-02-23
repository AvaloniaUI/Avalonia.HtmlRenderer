using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using HtmlRenderer.Demo.Avalonia;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Main(string[] args) => BuildAvaloniaApp()
        .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}