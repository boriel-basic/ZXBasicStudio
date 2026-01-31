using Avalonia;
using Avalonia.Svg.Skia;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace ZXBasicStudio
{
    internal class Program
    {
        public static string Version = "";
        public static string VersionDate = "";
      
      
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

            SetVerion();

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }


        public static void SetVerion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                var version = assembly.GetName().Version;
                Version = $"{version.Major}.{version.Minor}.{version.Build}";
                if (version.Revision != 0)
                {
                    Version = $"{Version} - beta {version.Revision}";
                }
                var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);
                VersionDate = buildDate.ToString("yyyy.MM.dd");
            }
            catch
            {
                Version = "Unknown version";
                VersionDate = "Unknown date";
            }
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