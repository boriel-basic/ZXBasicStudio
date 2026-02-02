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
                    SetVersion();
                }
                return _Version;
            }
        }
        private static string _Version = "";

        public static string VersionDate {             get
            {
                if (string.IsNullOrEmpty(_VersionDate))
                {
                    SetVersion();
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

            SetVersion();

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }


        public static void SetVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                var version = assembly.GetName().Version;
                _Version = $"{version.Major}.{version.Minor}.{version.Build}";
                if (version.Revision != 0)
                {
                    _Version = $"{_Version} - beta {version.Revision}";
                }
                
                string path = assembly.Location;
                if (string.IsNullOrEmpty(path))
                    path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                var buildDate = System.IO.File.GetLastWriteTime(path);
                _VersionDate = buildDate.ToString("yyyy.MM.dd");
            }
            catch
            {
                _Version = "Unknown";
                _VersionDate = "Unknown";
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