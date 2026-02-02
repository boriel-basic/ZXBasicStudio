using Avalonia;
using Avalonia.Svg.Skia;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace ZXBasicStudio
{
    internal class Program
    {
        public static string Version
        {
            get
            {
                if(string.IsNullOrEmpty(_Version))
                {
                    SetVerion();
                }
                return _Version;
            }
        }
        private static string _Version = "";

        public static string VersionDate {             get
            {
                if (string.IsNullOrEmpty(_VersionDate))
                {
                    SetVerion();
                }
                return _VersionDate;
            }
        }
        public static string _VersionDate = "";
      
      
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
                _Version = $"{version.Major}.{version.Minor}.{version.Build}";
                if (version.Revision != 0)
                {
                    _Version = $"{Version} - beta {version.Revision}";
                }
                var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);
                _VersionDate = buildDate.ToString("yyyy.MM.dd");
            }
            catch
            {
                _Version = "";
                _VersionDate = "";
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