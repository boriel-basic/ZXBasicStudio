using Avalonia;
using Avalonia.OpenGL;
using System;
using System.IO;
using System.Linq;

namespace ZXBSInstaller
{
    internal class Program
    {
        public static string Version = "";

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var version = assembly.GetName().Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            if (args.Contains("--version"))
            {
                Console.WriteLine($"{Version}");
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
