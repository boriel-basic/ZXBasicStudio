using Avalonia;
using Avalonia.Svg.Skia;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace ZXBasicStudio
{
    internal class Program
    {
        public static string Version = "1.6.0 - beta 5";
        public static string VersionDate = "2025.10.05";

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting= Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
            };

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            //Debugger.Launch();

            GC.KeepAlive(typeof(SvgImageExtension).Assembly);
            GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}