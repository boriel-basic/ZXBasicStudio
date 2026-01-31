using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZXBSInstaller.Log.Neg;
using static System.Environment;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ZXBSInstaller.Log
{
    /// <summary>
    /// ServiceLayer for installer and updater services
    /// </summary>
    public static class ServiceLayer
    {
        public static Config GeneralConfig = null;
        public static ExternalTool[] ExternalTools = null;
        public static OperatingSystems CurrentOperatingSystem = OperatingSystems.All;

        private static Action<string> ShowStatusPanel = null;
        private static Action<string, int> UpdateStatus = null;
        private static Action HideStatusPanel = null;
        private static Action RefreshTools = null;
        private static Action<string> ShowMessage = null;

        #region Constructor and tools

        /// <summary>
        /// Initilize the service layer
        /// </summary>
        /// <returns>True if correct or false if error</returns>
        public static bool Initialize(
            Action<string> callBackShowStatusPanel,
            Action<string, int> callBackUpdateStatus,
            Action callBackHideStatusPanel,
            Action callBackGetExternalTools,
            Action<string> callBackShowMessage)
        {
            ShowStatusPanel = callBackShowStatusPanel;
            UpdateStatus = callBackUpdateStatus;
            HideStatusPanel = callBackHideStatusPanel;
            RefreshTools = callBackGetExternalTools;
            ShowMessage = callBackShowMessage;

            GetConfig();

            if (OperatingSystem.IsWindows())
            {
                CurrentOperatingSystem = OperatingSystems.Windows;
            }
            else if (OperatingSystem.IsLinux())
            {
                CurrentOperatingSystem = OperatingSystems.Linux;
            }
            else if (OperatingSystem.IsMacOS())
            {
                CurrentOperatingSystem = OperatingSystems.MacOS;
            }

            return true;
        }


        public static Config GetConfig()
        {
            try
            {
                var filePath = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio", "ZXBSInstallerOptions.json");
                if (File.Exists(filePath))
                {
                    var jsonString = File.ReadAllText(filePath);
                    var cfg = JsonSerializer.Deserialize<Config>(jsonString);

                    cfg.ToolsListURL = "https://zx.duefectucorp.com/zxbsinstaller.json";

                    GeneralConfig = cfg;
                }
                else
                {
                    GeneralConfig = CreateConfig();
                    SaveConfig(GeneralConfig);
                }
                return GeneralConfig;

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static Config CreateConfig()
        {
            List<ExternalTools_Path> toolsPaths = null;
            var cfg = ServiceLayer.GeneralConfig;
            if (cfg == null)
            {
                cfg = new Config()
                {
                    BasePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName,
                    OnlyStableVersions = true,
                    SetZXBSConfig = true,
                    ToolsListURL = "https://zx.duefectucorp.com/zxbsinstaller.json"
                };
            }

            if (cfg.ExternalTools_Paths == null)
            {
                cfg.ExternalTools_Paths = new List<ExternalTools_Path>();
            }
            if (cfg.ExternalTools_Paths.Count == 0)
            {
                cfg.ExternalTools_Paths.Add(new ExternalTools_Path()
                {
                    Id = "zxbsinstaller",
                    LocalPath = Directory.GetCurrentDirectory()
                });
            }
            return cfg;
        }


        public static Config SaveConfig(Config config)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio");
                var fileName = Path.Combine(dir, "ZXBSInstallerOptions.json");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var jsonString = JsonSerializer.Serialize<Config>(config, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(fileName, jsonString);
                GeneralConfig = config;

                return GetConfig();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Convert an objet to his integer value or 0 if it can't do it
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Numeric value or 0 if can't do it</returns>
        private static int ToInteger(object value)
        {
            try
            {
                if (value == null)
                {
                    return 0;
                }
                int v = 0;
                if (int.TryParse(value.ToString(), out v))
                {
                    return v;
                }
            }
            catch { }
            return 0;
        }


        public static void OpenUrlInBrowser(string url)
        {
            try
            {
                switch (CurrentOperatingSystem)
                {
                    case OperatingSystems.Windows:
                        Process.Start(new ProcessStartInfo(url)
                        {
                            UseShellExecute = true
                        });
                        break;
                    case OperatingSystems.Linux:
                        Process.Start("xdg-open", url);
                        break;
                    case OperatingSystems.MacOS:
                        Process.Start("open", url);
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }

        #endregion


        #region External tools list

        /// <summary>
        /// Retrieves all external tools configured for use with the application.
        /// The data is stored in https://raw.githubusercontent.com/boriel-basic/ZXBasicStudio/refs/heads/master/externaltools.json
        /// </summary>
        /// <returns>An array of <see cref="ExternalTool"/> objects representing the available external tools. The array is empty
        /// if no external tools are configured or can download the config file.</returns>
        public static ExternalTool[] GetExternalTools()
        {
            UpdateStatus?.Invoke("Retrieving external tools information...", 5);

            using var httpClient = new HttpClient();
            string json = httpClient.GetStringAsync(GeneralConfig.ToolsListURL).GetAwaiter().GetResult();
            var tools = JsonSerializer.Deserialize<ExternalTool[]>(json);

            int max = tools.Length;
            int prg = 10;
            for (int n = 0; n < max; n++)
            {
                var tool = tools[n];
                prg = (n * 90) / max;
                UpdateStatus?.Invoke($"Retrieving versions for {tool.Name}...", prg + 10);

                tool.Versions = GetAvailableToolVersion(tool);
                if (tool.Versions == null)
                {
                    tool.Versions = new ExternalTools_Version[0];
                }
                tool.InstalledVersion = GetToolVersion(tool.Id);

                // Set latest version
                if (GeneralConfig.OnlyStableVersions)
                {
                    tool.LatestVersion = tool.Versions.
                        Where(d => d.OperatingSystem == CurrentOperatingSystem &&
                            d.BetaNumber == 0).
                        OrderByDescending(d => d.VersionNumber).
                        FirstOrDefault();
                }
                if (tool.LatestVersion == null || !GeneralConfig.OnlyStableVersions)
                {
                    tool.LatestVersion = tool.Versions.
                        Where(d => d.OperatingSystem == CurrentOperatingSystem).
                        OrderByDescending(d => d.VersionNumber).
                        FirstOrDefault();
                }

                // Path for first versions of ZXBSInstalller
                if (tool.Id == "zxbsinstaller" && tool.LatestVersion == null)
                {
                    tool.LatestVersion = tool.InstalledVersion;
                }

                // Determine whether you need to update
                if (tool.InstalledVersion == null)
                {
                    tool.UpdateNeeded = true;
                }
                else
                {
                    if (tool.LatestVersion != null)
                    {
                        if (tool.LatestVersion.VersionNumber > tool.InstalledVersion.VersionNumber)
                        {
                            tool.UpdateNeeded = true;
                        }
                    }
                }

                tool.LocalPath = Path.Combine(GeneralConfig.BasePath, tool.Id);
            }

            //GetPaths(ref tools);

            ExternalTools = tools.OrderBy(d => d.Order).ToArray();

            return ExternalTools;

#if GENERATE_JSON
            var lst = new List<ExternalTool>();
            // Compiler
            {
                UpdateStatus?.Invoke(null, 10);
                var tool = new ExternalTool()
                {
                    Id = "zxbasic",
                    Enabled = true,
                    Name = "Boriel ZX Basic Compiler",
                    Author = "Boriel",
                    Description = "ZXBCompiler is a ZX Spectrum BASIC cross compiler that translates ZX Spectrum BASIC code into optimized machine code, enabling faster execution and enhanced performance on ZX Spectrum systems. This tool is required to run and debug programs.",
                    DirectUpdate = true,
                    SupportedOperatingSystems = new OperatingSystems[] { OperatingSystems.Windows, OperatingSystems.Linux, OperatingSystems.MacOS },
                    SiteUrl = "https://boriel-basic.net",
                    LicenceUrl = "https://raw.githubusercontent.com/boriel-basic/zxbasic/refs/heads/main/LICENSE.txt",
                    LicenseType = "GNU Affero General Public License v3.0",
                    VersionsUrl = "https://boriel.com/files/zxb/",
                    Order = 1
                };
                UpdateStatus?.Invoke(null, 15);
                tool.Versions = GetBorielBasicVersions(tool.VersionsUrl);
                lst.Add(tool);
                UpdateStatus?.Invoke(null, 20);
            }

            // ZXBasic Studio IDE
            {
                UpdateStatus?.Invoke(null, 20);
                var tool = new ExternalTool()
                {
                    Id = "zxbs",
                    Enabled = true,
                    Name = "ZX Basic Studio",
                    Author = "Dr.Gusman, Boriel, Duefectu, AdolFITO, Hash6Iron and SirRickster",
                    Description = "IDE (Integrated Development Environment) with Boriel Basic code editor, Assembler, UDGs, fonts, sprites, .tap editor, debugger, emulator, etc. This tool is optional but highly recommended.",
                    DirectUpdate = true,
                    SupportedOperatingSystems = new OperatingSystems[] { OperatingSystems.Windows, OperatingSystems.Linux, OperatingSystems.MacOS },
                    SiteUrl = "https://github.com/boriel-basic/ZXBasicStudio",
                    LicenceUrl = "https://raw.githubusercontent.com/boriel-basic/ZXBasicStudio/refs/heads/master/LICENSE.txt",
                    LicenseType = "MIT License",
                    VersionsUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/",
                    Order=2
                };
                UpdateStatus?.Invoke(null, 25);
                // Versions
                var versions = new List<ExternalTools_Version>();
                versions.Add(new ExternalTools_Version()
                {
                    Version = "1.6.0-beta5",
                    BetaNumber = 5,
                    VersionNumber = 1006005,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-linux-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.Linux,
                });
                versions.Add(new ExternalTools_Version()
                {
                    Version = "1.6.0-beta5",
                    BetaNumber = 5,
                    VersionNumber = 1006005,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-osx-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.MacOS,
                });
                versions.Add(new ExternalTools_Version()
                {
                    Version = "1.6.0-beta5",
                    BetaNumber = 5,
                    VersionNumber = 1006005,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-win-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.Windows,
                });
                tool.Versions = versions.ToArray();
                lst.Add(tool);
                UpdateStatus?.Invoke(null, 30);
            }
            // Get tools paths from ZXBS config file
            GetPaths(ref lst);


            // ZXBasic Studio Installer
            {
                UpdateStatus?.Invoke(null, 30);
                var tool = new ExternalTool()
                {
                    Id = "zxbsinstaller",
                    Enabled = true,
                    Name = "ZX Basic Studio Installer",
                    Author = "Duefectu",
                    Description = "This program, and it is used to download, install and keep all external tools and ZX Basic Studio itself up to date.",
                    DirectUpdate = true,
                    SupportedOperatingSystems = new OperatingSystems[] { OperatingSystems.Windows, OperatingSystems.Linux, OperatingSystems.MacOS },
                    SiteUrl = "https://github.com/boriel-basic/ZXBasicStudio",
                    LicenceUrl = "https://raw.githubusercontent.com/boriel-basic/ZXBasicStudio/refs/heads/master/LICENSE.txt",
                    LicenseType = "MIT License",
                    VersionsUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/",
                    Order = 0
                };
                UpdateStatus?.Invoke(null, 35);
                // Versions
                var versions = new List<ExternalTools_Version>();
                versions.Add(new ExternalTools_Version()
                {
                    Version = "0.0.1-beta1",
                    BetaNumber = 1,
                    VersionNumber = 1001,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-linux-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.Linux,
                });
                versions.Add(new ExternalTools_Version()
                {
                    Version = "0.0.1-beta1",
                    BetaNumber = 1,
                    VersionNumber = 1001,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-osx-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.MacOS,
                });
                versions.Add(new ExternalTools_Version()
                {
                    Version = "0.0.1-beta1",
                    BetaNumber = 1,
                    VersionNumber = 1001,
                    DownloadUrl = "https://github.com/boriel-basic/ZXBasicStudio/releases/download/v1.6/ZXBasicStudio-win-x64.v1.6.0-beta5.zip",
                    OperatingSystem = OperatingSystems.Windows,
                });
                tool.Versions = versions.ToArray();
                lst.Add(tool);
                UpdateStatus?.Invoke(null, 40);
            }
            // Get tools paths from ZXBS config file
            GetPaths(ref lst);

            externalTools = lst.OrderBy(d=>d.Order).ToArray();
            
            var test=JsonSerializer.Serialize(externalTools);
            File.WriteAllText(@"c:\temp\zxbsinstaller.json",test);

            return externalTools;
#endif
        }



        public static void GetPaths(ref ExternalTool[] tools)
        {
            var filePath = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio", "ZXBasicStudioOptions.json");
            if (!File.Exists(filePath))
            {
                return;
            }
            var jsonString = File.ReadAllText(filePath);
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            UpdatePath("zxbasic", "ZxbcPath", root, ref tools);
            // ZX Basic Studio
            {
                var tool = tools.FirstOrDefault(t => t.Id == "zxbs");
                if (tool != null)
                {
                    tool.LocalPath = Directory.GetCurrentDirectory();
                }
            }
            // ZX Basic Studio Installer
            {
                var tool = tools.FirstOrDefault(t => t.Id == "zxbsinstaller");
                if (tool != null)
                {
                    tool.LocalPath = Directory.GetCurrentDirectory();
                }
            }
        }


        private static void UpdatePath(string toolId, string property, JsonElement root, ref ExternalTool[] tools)
        {
            var tool = tools.FirstOrDefault(t => t.Id == toolId);
            if (tool == null)
            {
                return;
            }
            if (root.TryGetProperty(property, out JsonElement element))
            {
                string value = element.GetString();
                tool.FullLocalPath = value;
                var fn = Path.GetFileName(value);
                value = value.Replace(fn, "");
                tool.LocalPath = value;
            }
        }


        private static (int, int) GetVersionNumber(string versionString)
        {
            int number = 0;
            int betaNumber = 0;
            string version = versionString;
            if (version.Contains("-beta"))
            {
                var mv = Regex.Match(version, @"beta(\d+)(?:[-\.]|$)", RegexOptions.IgnoreCase);
                if (mv.Success)
                {
                    version = version.Replace("-beta", ".");
                }
            }
            else
            {
                version += ".0";
            }

            var versionParts = version.Split(".");
            if (versionParts.Length == 5)
            {
                versionParts[3] += versionParts[4];
            }
            for (int n = 0; n < 4; n++)
            {
                number *= 1000;
                if (n < versionParts.Length)
                {
                    int v = ToInteger(versionParts[n]);
                    if (n == 3)
                    {
                        betaNumber = v;
                        if (betaNumber == 0)
                        {
                            number += 999;
                        }
                        else
                        {
                            number += betaNumber;
                        }
                    }
                    else
                    {
                        number += v;
                    }
                }
            }
            return (number, betaNumber);
        }

        #endregion


        #region External tools versions retrieval

        private static ExternalTools_Version[] GetAvailableToolVersion(ExternalTool tool)
        {
            switch (tool.Id)
            {
                case "zxbasic":
                    return GetBorielBasicVersions(tool.VersionsUrl);

                case "zxbs":
                    return GetBorielZXBSVersions(tool.VersionsUrl, false);

                case "zxbsinstaller":
                    return GetBorielZXBSVersions(tool.VersionsUrl, true);

                default:
                    return null;
            }
        }


        /// <summary>
        /// Get versions data for Boriel Basic Compiler
        /// </summary>
        /// <param name="versionsUrl"></param>
        /// <returns></returns>
        private static ExternalTools_Version[] GetBorielBasicVersions(string versionsUrl)
        {
            try
            {
                var links = GetAllLinks(versionsUrl, @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""']");

                // Parse links extracting versions data
                var lst = new List<ExternalTools_Version>();
                Regex _regex = new Regex(
                      @"zxbasic-(?:v)?(?<version>\d+\.\d+\.\d+(?:-beta\d+)?)",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase
                );
                foreach (var link in links)
                {
                    // Search version, beta and platform
                    var match = _regex.Match(link);
                    if (!match.Success)
                    {
                        continue;
                    }
                    var version = new ExternalTools_Version();
                    // Download url
                    var uri = new Uri(versionsUrl);
                    version.DownloadUrl = $"https://{uri.Host}{link}";
                    // Display version
                    version.Version = match.Groups["version"].Value;
                    // Platform
                    if (link.Contains("win"))
                    {
                        version.OperatingSystem = OperatingSystems.Windows;
                    }
                    else if (link.Contains("linux"))
                    {
                        version.OperatingSystem = OperatingSystems.Linux;
                    }
                    else if (link.Contains("mac"))
                    {
                        version.OperatingSystem = OperatingSystems.MacOS;
                    }
                    else
                    {
                        version.OperatingSystem = OperatingSystems.All;
                    }
                    // Version number
                    var ver = GetVersionNumber(version.Version);
                    version.VersionNumber = ver.Item1;
                    // IsEstable
                    version.BetaNumber = ver.Item2;
                    // Add to list
                    lst.Add(version);
                }
                // Return the list ordered by version number
                return lst.OrderByDescending(d => d.VersionNumber).ToArray();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Get versions data for ZX Basic Studio Compiler
        /// </summary>
        /// <param name="versionsUrl"></param>
        /// <returns></returns>
        private static ExternalTools_Version[] GetBorielZXBSVersions(string versionsUrl, bool installer)
        {
            try
            {
                // Get all hrefs
                var links = GetAllLinks(versionsUrl, @"href=""([^""]+)""");
                // Get only releases
                links = links.Where(d => d.Contains("/boriel-basic/ZXBasicStudio/releases/tag/")).ToArray();

                var versions = new List<ExternalTools_Version>();
                foreach (var link in links)
                {
                    var url = link.Replace("/boriel-basic/ZXBasicStudio/releases/tag/", "");
                    url = $"https://github.com/boriel-basic/ZXBasicStudio/releases/expanded_assets/{url}";
                    var filesLinks = GetAllLinks(url, @"href=""([^""]+)""");
                    foreach (var fl in filesLinks)
                    {
                        if (fl.Contains("download"))
                        {
                            if (installer)
                            {
                                if (!fl.Contains("zxbsinstaller"))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (fl.Contains("zxbsinstaller"))
                                {
                                    continue;
                                }
                            }
                            var version = GetGitHubZXBSVersion(fl);
                            if (version != null)
                            {
                                versions.Add(version);
                            }
                        }
                    }
                }
                return versions.ToArray();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private static ExternalTools_Version GetGitHubZXBSVersion(string fileLink)
        {
            try
            {
                var version = new ExternalTools_Version();
                version.DownloadUrl = $"https://github.com{fileLink}";

                // Operating system
                if (fileLink.Contains("win"))
                {
                    version.OperatingSystem = OperatingSystems.Windows;
                }
                else if (fileLink.Contains("linux"))
                {
                    version.OperatingSystem = OperatingSystems.Linux;
                }
                else if (fileLink.Contains("osx") || fileLink.Contains("mac"))
                {
                    version.OperatingSystem = OperatingSystems.MacOS;
                }
                else
                {
                    return null;
                }

                // Version
                var match = Regex.Match(fileLink, @"v(\d+(?:\.\d+)*(?:-[\w\d.]+)?)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    version.Version = match.Groups[1].Value;
                }
                if (!fileLink.Contains("zxbsinstaller"))
                {
                    // Path old version numbers
                    switch (version.Version)
                    {
                        case "1.6":
                            version.Version = "1.6.0-beta5";
                            break;
                        case "1.5":
                            version.Version = "1.5.0-beta1";
                            break;
                    }
                }
                var v = GetVersionNumber(version.Version);
                version.VersionNumber = v.Item1;
                version.BetaNumber = v.Item2;
                return version;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private static string[] GetAllLinks(string url, string pattern)
        {
            // Get html file
            string html;
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            using (HttpClient client = new HttpClient(handler))
            {
                html = client.GetStringAsync(url).GetAwaiter().GetResult();
            }
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }
            //File.WriteAllText("c:/temp/html.text", html);
            // Get links
            var links = new List<string>();
            {
                var regex = new Regex(
                    pattern,
                    RegexOptions.IgnoreCase);
                var matches = regex.Matches(html);
                foreach (Match match in matches)
                {
                    links.Add(match.Groups[1].Value);
                }
            }

            return links.ToArray();
        }

        #endregion


        #region Local tools versions

        public static ExternalTools_Version GetToolVersion(string id)
        {
            try
            {
                var dir = Path.Combine(GeneralConfig.BasePath, id);

                switch (id)
                {
                    case "zxbasic":
                        return GetBorielBasicVersion(dir);
                    case "zxbs":
                        return GetZXBSVersion(dir);
                    case "zxbsinstaller":
                        return GetZXBSInstallerVersion(dir);
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private static ExternalTools_Version GetBorielBasicVersion(string exePath)
        {
            try
            {
                var fileName = Path.Combine(exePath, "zxbc.exe");
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(exePath, "zxbc");
                }
                if (!File.Exists(fileName))
                {
                    return null;
                }
                // Launch "zxbc.exe --version"
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using Process process = new Process { StartInfo = psi };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrEmpty(output))
                {
                    return null;
                }
                var version = output.Replace("zxbc.py ", "").Replace("\n", "").Replace("\r", "").Replace("v", "");
                var v = GetVersionNumber(version);
                int number = v.Item1;
                int beta = v.Item2;

                return new ExternalTools_Version()
                {
                    DownloadUrl = "",
                    BetaNumber = beta,
                    OperatingSystem = OperatingSystems.All,
                    Version = version,
                    VersionNumber = number
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private static ExternalTools_Version GetZXBSVersion(string exePath)
        {
            try
            {
                var fileName = Path.Combine(exePath, "ZXBasicStudio.exe");
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(exePath, "ZXBasicStudio");
                }
                if (!File.Exists(fileName))
                {
                    return null;
                }

                var fvi = FileVersionInfo.GetVersionInfo(fileName);
                if (fvi != null)
                {
                    // Major, minor, Build, private
                    var version = $"{fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductBuildPart}";
                    if (fvi.ProductPrivatePart > 0)
                    {
                        version += $"-beta{fvi.ProductPrivatePart}";
                    }
                    if (version == "1.0.0")
                    {
                        version = "1.6.0-beta6.3";
                    }
                    var v = GetVersionNumber(version);
                    var versionNumber = v.Item1;
                    var beta = v.Item2;
                    return new ExternalTools_Version()
                    {
                        DownloadUrl = "",
                        BetaNumber = beta,
                        OperatingSystem = OperatingSystems.All,
                        Version = version,
                        VersionNumber = versionNumber
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static ExternalTools_Version GetZXBSInstallerVersion(string exePath)
        {
            try
            {
                var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
                var parts = assemblyVersion.Split('.');
                if (parts.Length < 4)
                {
                    return null;
                }
                ;
                var version = $"{parts[0]}.{parts[1]}.{parts[2]}";
                var beta = ToInteger(parts[3]);
                if (beta > 0)
                {
                    version += $"-beta{beta}";
                }
                var v = GetVersionNumber(version);
                return new ExternalTools_Version()
                {
                    DownloadUrl = "",
                    BetaNumber = v.Item2,
                    OperatingSystem = OperatingSystems.All,
                    Version = version,
                    VersionNumber = v.Item1
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion


        #region Install external tool

        public static void DownloadAndInstallTools()
        {
            ShowStatusPanel($"Working...");
            foreach (var tool in ExternalTools)
            {
                if (tool.IsSelected)
                {
                    DownloadAndInstallTool(tool, tool.LatestVersion);
                }
            }
            HideStatusPanel();
            RefreshTools();
        }


        public static void DownloadAndInstallTool(ExternalTool tool, ExternalTools_Version version)
        {
            string step = "";
            try
            {
                ShowStatusPanel($"Downloading {tool.Name} version {version.Version}...");

                // Download path
                step = "Creating download path";
                var tempFile = Path.Combine(GeneralConfig.BasePath, "downloads");
                if (!Directory.Exists(tempFile))
                {
                    step = $"Creating download path [{tempFile}]";
                    Directory.CreateDirectory(tempFile);
                }
                var uri = new Uri(version.DownloadUrl);
                tempFile = Path.Combine(tempFile, Path.GetFileName(uri.LocalPath));

                // Get installation path
                step = "Creating installation path";
                var installationPath = Path.Combine(GeneralConfig.BasePath, tool.Id);
                // Patch for Boriel Basic
                if (tool.Id == "zxbasic")
                {
                    installationPath = Directory.GetParent(installationPath).FullName;
                }
                if (!Directory.Exists(installationPath))
                {
                    step = $"Creating application path [{installationPath}]";
                    Directory.CreateDirectory(installationPath);
                }

                // Download file
                step = $"Downloading file {version.DownloadUrl}";
                UpdateStatus($"Downloading {version.DownloadUrl}", 50);
                using (var httpClient = new HttpClient())
                {
                    using (var response = httpClient.GetAsync(version.DownloadUrl).GetAwaiter().GetResult())
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                        }
                    }
                }

                // Extract file
                step = $"Installing {tool.Name}";
                UpdateStatus($"Installing {tool.Name} version {version.Version}...", 50);
                ExtractFile(tempFile, installationPath);

                // Set ZXBS Options
                step = "Set ZX Basic Studio options";
                UpdateStatus(null, 75);
                SetZXBSConfig();

                // Delete temp file
                step = "Deleting temp files";
                UpdateStatus("Deleting temp files...", 90);
                File.Delete(tempFile);

                UpdateStatus($"{tool.Name} version {version.Version} installed successfully.", 100);
            }
            catch (Exception ex)
            {
                HideStatusPanel();
                ShowMessage($"Error installing {tool.Name}\r\n{step}\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }


        private static void ExtractFile(string archive, string destination)
        {
            if (archive.ToLower().EndsWith(".zip"))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archive, destination, true);
            }
            else if (CurrentOperatingSystem != OperatingSystems.Windows)
            {
                Directory.CreateDirectory(destination);

                var psi = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xzf \"{archive}\" -C \"{destination}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi)!;

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    ShowMessage($"Error unpacking file {archive}\r\n{stderr}");
                    return;
                }
            }
            //if (archive.ToLower().EndsWith(".tar.gz"))
            //{
            //    Directory.CreateDirectory(destination);

            //    using var fileStream = File.OpenRead(archive);
            //    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            //    using var tarReader = new TarReader(gzipStream);

            //    TarEntry? entry;
            //    while ((entry = tarReader.GetNextEntry()) != null)
            //    {
            //        string fullPath = Path.Combine(destination, entry.Name);

            //        if (entry.EntryType == TarEntryType.Directory)
            //        {
            //            Directory.CreateDirectory(fullPath);
            //        }
            //        else
            //        {
            //            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            //            if (entry.Length == 0)
            //            {
            //                File.WriteAllBytes(fullPath, new byte[0]);
            //            }
            //            else
            //            {
            //                entry.DataStream!.CopyTo(File.Create(fullPath));
            //            }
            //        }
            //    }
            //}
        }


        private static void SetZXBSConfig()
        {
            try
            {
                var filePath = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio", "ZXBasicStudioOptions.json");
                if (!File.Exists(filePath))
                {
                    string data = @"{
  ""ZxbasmPath"": """",
  ""ZxbcPath"": """",
  ""EditorFontSize"": 16.0,
  ""WordWrap"": true,
  ""AudioDisabled"": false,
  ""Cls"": true,
  ""Borderless"": false,
  ""AntiAlias"": false,
  ""LastProjectPath"": """",
  ""DefaultBuildSettings"": null,
  ""NextEmulatorPath"": """",
  ""DisableAuto"": true
}";
                    File.WriteAllText(filePath, data);
                }

                var sb = new StringBuilder();
                var sr = new StreamReader(filePath);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    // Set values
                    if (line.Contains("ZxbasmPath"))
                    {
                        string exe = "zxbasm";
                        if (CurrentOperatingSystem == OperatingSystems.Windows)
                        {
                            exe += ".exe";
                        }
                        var dir = Path.Combine(GeneralConfig.BasePath, "zxbasic", exe).Replace("\\", "\\\\");
                        line = $"  \"ZxbasmPath\": \"{dir}\",";
                    }
                    else if (line.Contains("ZxbcPath"))
                    {
                        string exe = "zxbc";
                        if (CurrentOperatingSystem == OperatingSystems.Windows)
                        {
                            exe += ".exe";
                        }
                        var dir = Path.Combine(GeneralConfig.BasePath, "zxbasic", exe).Replace("\\", "\\\\");
                        line = $"  \"ZxbcPath\": \"{dir}\",";
                    }
                    sb.AppendLine(line);
                }
                sr.Close();
                sr.Dispose();
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                //ShowMessage($"Error updating ZX Basic Studio options.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }


        #endregion

    }
}