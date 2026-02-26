using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
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
        /// <summary>
        /// Application configuration. It is stored in %AppData%\ZXBasicStudio\ZXBSInstallerOptions.json
        /// </summary>
        public static Config GeneralConfig = null;
        /// <summary>
        /// List of external tools.
        /// </summary>
        public static ExternalTool[] ExternalTools = null;
        /// <summary>
        /// Current operating system
        /// </summary>
        public static OperatingSystems CurrentOperatingSystem = OperatingSystems.All;
        /// <summary>
        /// True if the computer is a Mac
        /// </summary>
        public static bool IsMac = false;
        /// <summary>
        /// Used to cancel the current operation. It is set to true when the user clicks the cancel button and it is checked in all long operations to stop them if it is true.
        /// </summary>
        public static bool Cancel = false;


        // Callbacks to update the UI from the service layer
        /// <summary>
        /// Show the status panel with a message. The message is used to inform the user about the current operation being performed. It is called from the service layer to update the UI.
        /// </summary>
        private static Action<string> ShowStatusPanel = null;
        /// <summary>
        /// Update the status message and progress. It is called from the service layer to update the UI. The message is used to inform the user about the current operation being performed and the progress is a value between 0 and 100 that indicates the progress of the current operation.
        /// </summary>
        private static Action<string, int> UpdateStatus = null;
        /// <summary>
        /// Hide the status panel. It is called from the service layer to update the UI when the current operation is finished or cancelled.
        /// </summary>
        private static Action HideStatusPanel = null;
        /// <summary>
        /// Refresh the list of external tools. It is called from the service layer to update the UI when the list of external tools is updated, for example, after installing or updating a tool.
        /// </summary>
        private static Action RefreshTools = null;
        /// <summary>
        /// Show a message in a dialog window. It is called from the service layer to show error messages or any other information that needs to be shown to the user.
        /// </summary>
        private static Action<string> ShowMessage = null;
        /// <summary>
        /// Exit the application. It is called from the service layer to exit the application when launching ZXBasicStudio. 
        /// </summary>
        private static Action ExitApp = null;


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
            Action<string> callBackShowMessage,
            Action callBackExitApp)
        {
            try
            {
                // Set callbacks
                ShowStatusPanel = callBackShowStatusPanel;
                UpdateStatus = callBackUpdateStatus;
                HideStatusPanel = callBackHideStatusPanel;
                RefreshTools = callBackGetExternalTools;
                ShowMessage = callBackShowMessage;
                ExitApp = callBackExitApp;

                // Retrive the configuration
                GetConfig();

                // Set current operating system
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
                    IsMac = true;
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        CurrentOperatingSystem = OperatingSystems.MacOS_arm64;
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    {
                        CurrentOperatingSystem = OperatingSystems.MacOS_x64;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error initializing.\r\n{ex.Message}{ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        /// Get the config from file or create a new one if it doesn't exist. The config file is stored in %AppData%\ZXBasicStudio\ZXBSInstallerOptions.json and it contains the configuration for the installer, such as the URL to retrieve the external tools list, the base path to install the tools, etc.
        /// </summary>
        /// <returns>Config data. ServiceLayer.GeneralConfig is set</returns>
        private static Config GetConfig()
        {
            try
            {
                // Build filePath
                var filePath = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio", "ZXBSInstallerOptions.json");
                if (File.Exists(filePath))
                {
                    // Read config from file
                    var jsonString = File.ReadAllText(filePath);
                    var cfg = JsonSerializer.Deserialize<Config>(jsonString);

                    cfg.ToolsListURL = "https://zx.duefectucorp.com/zxbsinstaller.json";

                    GeneralConfig = cfg;
                }
                else
                {
                    // Create default config and save it to file
                    GeneralConfig = CreateConfig();
                    SaveConfig(GeneralConfig);
                }
                return GeneralConfig;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error retrieving configuration.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Create the default config
        /// </summary>
        /// <returns>Config object with the default config</returns>
        private static Config CreateConfig()
        {
            try
            {
                List<ExternalTools_Path> toolsPaths = null;
                var cfg = ServiceLayer.GeneralConfig;
                if (cfg == null)
                {
                    // Create base config
                    cfg = new Config()
                    {
                        BasePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName,
                        OnlyStableVersions = true,
                        SetZXBSConfig = true,
                        ToolsListURL = "https://zx.duefectucorp.com/zxbsinstaller.json"
                    };
                }

                // Fill paths
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
            catch (Exception ex)
            {
                ShowMessage($"Error creating default configuration.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Save config to file. The config file is stored in %AppData%\ZXBasicStudio\ZXBSInstallerOptions.json and it contains the configuration for the installer, such as the URL to retrieve the external tools list, the base path to install the tools, etc. ServiceLayer.GeneralConfig is updated with the new config data.
        /// </summary>
        /// <param name="config">Config to store</param>
        /// <returns>Config with the current configuration</returns>
        public static Config SaveConfig(Config config)
        {
            try
            {
                // Build dir and file path
                var dir = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio");
                var fileName = Path.Combine(dir, "ZXBSInstallerOptions.json");
                if (!Directory.Exists(dir))
                {
                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(dir);
                }
                // Save config to file
                var jsonString = JsonSerializer.Serialize<Config>(config, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(fileName, jsonString);
                GeneralConfig = config;

                return GetConfig();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error saving configuration.\r\n{ex.Message}{ex.StackTrace}");
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


        /// <summary>
        /// Open an url in the default browser. It is used to open the site and license urls of the external tools. It is called from the service layer when the user clicks on the site or license buttons of an external tool.
        /// </summary>
        /// <param name="url">Url to open</param>
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
                    case OperatingSystems.MacOS_x64:
                    case OperatingSystems.MacOS_arm64:
                        Process.Start("open", url);
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error opening {url}.\r\n{ex.Message}{ex.StackTrace}");
            }
        }

        #endregion


        #region External tools list

        /// <summary>
        /// Retrieves all external tools configured for use with the application.
        /// </summary>
        /// <returns>An array of <see cref="ExternalTool"/> objects representing the available external tools. The array is empty
        /// if no external tools are configured or can download the config file.</returns>
        public static ExternalTool[] GetExternalTools()
        {
            try
            {
                UpdateStatus?.Invoke("Retrieving external tools information...", 5);

                // Get external tools list from embded resource
                ExternalTool[] tools = null;
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using (Stream? stream = assembly.GetManifestResourceStream("ZXBSInstaller.Log.ExternalTools.json"))
                    {
                        if (stream == null)
                        {
                            ShowMessage("ERROR, unable to obtain the list of external tools. Download and install a new version of ZXBSInstaller: ERROR #1");
                            return null;
                        }
                        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        var json = reader.ReadToEnd();
                        if (string.IsNullOrEmpty(json))
                        {
                            ShowMessage("ERROR, unable to obtain the list of external tools. Download and install a new version of ZXBSInstaller: ERROR #2");
                            return null;
                        }
                        tools = JsonSerializer.Deserialize<ExternalTool[]>(json);
                        if (tools == null)
                        {
                            ShowMessage("ERROR, unable to obtain the list of external tools. Download and install a new version of ZXBSInstaller. ERROR #3");
                            return null;
                        }
                    }
                }

                int max = tools.Length;
                int prg = 10;
                for (int n = 0; n < max; n++)
                {
                    // Cancel?
                    if (Cancel)
                    {
                        return null;
                    }
                    // Update status
                    var tool = tools[n];
                    prg = (n * 90) / max;
                    UpdateStatus?.Invoke($"Retrieving versions for {tool.Name}...", prg + 10);

                    // Get available versions for tool
                    tool.Versions = GetAvailableToolVersion(tool);
                    if (tool.Versions == null)
                    {
                        tool.Versions = new ExternalTools_Version[0];
                    }
                    // Get installed version
                    tool.InstalledVersion = GetToolVersion(tool.Id);

                    // Set latest version
                    if (GeneralConfig.OnlyStableVersions)
                    {
                        if (tool.Id == "zxbasic" && IsMac)
                        {
                            tool.LatestVersion = tool.Versions.
                                Where(d => d.OperatingSystem == OperatingSystems.MacOS &&
                                    d.BetaNumber == 0).
                                OrderByDescending(d => d.VersionNumber).
                                FirstOrDefault();
                        }
                        else
                        {
                            tool.LatestVersion = tool.Versions.
                                Where(d => d.OperatingSystem == CurrentOperatingSystem &&
                                    d.BetaNumber == 0).
                                OrderByDescending(d => d.VersionNumber).
                                FirstOrDefault();
                        }
                    }
                    if (tool.LatestVersion == null || !GeneralConfig.OnlyStableVersions)
                    {
                        if (tool.Id == "zxbasic" && IsMac)
                        {
                            tool.LatestVersion = tool.Versions.
                                Where(d => d.OperatingSystem == OperatingSystems.MacOS).
                                OrderByDescending(d => d.VersionNumber).
                                FirstOrDefault();
                        }
                        else
                        {
                            tool.LatestVersion = tool.Versions.
                                Where(d => d.OperatingSystem == CurrentOperatingSystem).
                                OrderByDescending(d => d.VersionNumber).
                                FirstOrDefault();
                        }
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
                    // Set tool local path
                    tool.LocalPath = Path.Combine(GeneralConfig.BasePath, tool.Id);
                }

                // order tools by order property
                ExternalTools = tools.OrderBy(d => d.Order).ToArray();
                return ExternalTools;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error retrieving external tools information. Please check your internet connection and try again.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get version numeric value and beta value from version string. 
        /// Four values are used, the last one identifying the beta version number. If it is 0, it is the stable version.
        /// 1.2.3.0 = version 1.2.3 stable
        /// 1.2.3.4 = version 1.2.3 beta 4
        /// Values are multiplied by 1000 to get a single integer value that can be compared easily. For example:
        /// 1.2.3.0 = 1*1000*1000*1000 + 2*1000*1000 + 3*1000 + 0 = 1002003000
        /// 1.2.3.4 = 1*1000*1000*1000 + 2*1000*1000 + 3*1000 + 4 = 1002003004
        /// </summary>
        /// <param name="versionString">Version string</param>
        /// <returns>Item1 = version number, Item2 = beta number</returns>
        private static (int, int) GetVersionNumber(string versionString)
        {
            try
            {
                int number = 0;
                int betaNumber = 0;
                string version = versionString;
                // If it is a beta version, replace the -beta with . and add the beta number as the fourth value.
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

                // Split version string
                var versionParts = version.Split(".");
                if (versionParts.Length == 5)
                {
                    versionParts[3] += versionParts[4];
                }
                // Get value for each part of the version string
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
            catch (Exception ex)
            {
                ShowMessage($"Error parsing version number.\r\n{ex.Message}{ex.StackTrace}");
                return (0, 0);
            }
        }

        #endregion


        #region External tools versions retrieval

        /// <summary>
        /// Get the versions available for the specified tool. 
        /// The versions are retrieved from the tool's VersionsUrl property, which is configured in the external tools list. 
        /// The method parses the HTML of the VersionsUrl page to extract the available versions and their download URLs. 
        /// The parsing is specific for each tool, depending on how the versions are listed in the page. 
        /// The method returns an array of ExternalTools_Version objects that contain the version information, such as version number, beta number, download URL and operating system. 
        /// If there is an error retrieving or parsing the versions, it returns null and shows an error message to the user.
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        private static ExternalTools_Version[] GetAvailableToolVersion(ExternalTool tool)
        {
            try
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
            catch (Exception ex)
            {
                ShowMessage($"Error getting available versions.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get versions data for Boriel Basic Compiler
        /// </summary>
        /// <param name="versionsUrl">Repository URL</param>
        /// <returns>Array of versions</returns>
        private static ExternalTools_Version[] GetBorielBasicVersions(string versionsUrl)
        {
            try
            {
                // Get all hrefs in the page according to the pattern.
                // The pattern is specific for the Boriel Basic repository page, which contains links to the versions in the format <a href="/boriel-basic/zxbasic/releases/download/v1.2.3/zxbasic-v1.2.3-win.zip">zxbasic-v1.2.3-win.zip</a>
                var links = GetAllLinks(versionsUrl, @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""']");
                if (links == null)
                {
                    ShowMessage("The Boriel Basic repository could not be accessed. Please check your internet connection or try again later.");
                    return null;
                }
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
                ShowMessage($"Error retrieving Boriel Basic versions.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get versions data for ZX Basic Studio Compiler
        /// </summary>
        /// <param name="versionsUrl">Repository URL</param>
        /// <param name="installer">True to get installer versions or false to get ZXBS versions</param>
        /// <returns>Array of versions</returns>
        private static ExternalTools_Version[] GetBorielZXBSVersions(string versionsUrl, bool installer)
        {
            try
            {
                // Get all hrefs
                var links = GetAllLinks(versionsUrl, @"href=""([^""]+)""");
                if (links == null)
                {
                    ShowMessage("The ZX Basic Studio repository could not be accessed. Please check your internet connection or try again later.");
                    return null;
                }

                // Get only releases
                links = links.Where(d => d.Contains("/boriel-basic/ZXBasicStudio/releases/tag/")).ToArray();

                // Process all links
                var versions = new List<ExternalTools_Version>();
                foreach (var link in links)
                {
                    var url = link.Replace("/boriel-basic/ZXBasicStudio/releases/tag/", "");
                    url = $"https://github.com/boriel-basic/ZXBasicStudio/releases/expanded_assets/{url}";
                    var filesLinks = GetAllLinks(url, @"href=""([^""]+)""");
                    if (filesLinks == null)
                    {
                        if (installer)
                        {
                            ShowMessage("The ZXBSInstaller repository could not be accessed. Please check your internet connection or try again later.");
                        }
                        else
                        {
                            ShowMessage("The ZX Basic Studio repository could not be accessed. Please check your internet connection or try again later.");
                        }
                        return null;
                    }
                    foreach (var fl in filesLinks)
                    {
                        if (fl.Contains("download"))
                        {
                            if (installer)
                            {
                                if (!fl.Contains("ZXBSInstaller"))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (fl.Contains("ZXBSInstaller"))
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
                ShowMessage($"Error retrieving ZXBS/Installer versions.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get version based on the link for ZXBS and ZXBSInstaller.
        /// </summary>
        /// <param name="fileLink">Link of the version</param>
        /// <returns>Version info</returns>
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
                else if (fileLink.Contains("osx-x64"))
                {
                    version.OperatingSystem = OperatingSystems.MacOS_x64;
                }
                else if (fileLink.Contains("osx-arm64"))
                {
                    version.OperatingSystem = OperatingSystems.MacOS_arm64;
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
                ShowMessage($"Error parsing ZXBS version.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get all links of an url based on a regex pattern. 
        /// It is used to parse the HTML of the versions page of the tools to extract the links of the versions. 
        /// The pattern is specific for each tool, depending on how the versions are listed in the page.
        /// </summary>
        /// <param name="url">Url to retrive</param>
        /// <param name="pattern">Regex patter for the urls</param>
        /// <returns>Array of links in string format</returns>
        private static string[] GetAllLinks(string url, string pattern)
        {
            try
            {
                // Get html file
                string html = "";
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true
                };
                // Download page with retries in case of network errors or timeouts
                int retries = 5;
                while (retries-- > 0)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.Timeout = TimeSpan.FromSeconds(20);
                            html = client.GetStringAsync(url).GetAwaiter().GetResult();
                        }
                    }
                    catch { }
                    if (!string.IsNullOrEmpty(html))
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }
                if (string.IsNullOrEmpty(html))
                {
                    return null;
                }

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
            catch (Exception ex)
            {
                ShowMessage($"Error getting links.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }

        }

        #endregion


        #region Local tools versions

        /// <summary>
        /// Get local version of one external tool
        /// </summary>
        /// <param name="id">Tool id</param>
        /// <returns>Installed version</returns>
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
            }
            catch (Exception ex)
            {
                ShowMessage($"Error retrieving local version for {id}.\r\n{ex.Message}{ex.StackTrace}");
            }
            return null;
        }


        /// <summary>
        /// Get installed Boriel ZX Basic Compiler version
        /// </summary>
        /// <param name="exePath">Path of the tool</param>
        /// <returns>Version info</returns>
        private static ExternalTools_Version GetBorielBasicVersion(string exePath)
        {
            try
            {
                // Windows fileName
                var fileName = Path.Combine(exePath, "zxbc.exe");
                if (!File.Exists(fileName))
                {
                    // If not exist, try Linux/Mac fileName
                    fileName = Path.Combine(exePath, "zxbc.py");
                }
                if (!File.Exists(fileName))
                {
                    // Not found
                    return null;
                }
                // Retrieve version executing with --version parameter
                return GetVersionFromParameter(fileName);
            }
            catch (Exception ex)
            {
                ShowMessage($"Error getting local Boriel Basic version.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get intalled ZX Basic Studio version.
        /// The version is stored in version.txt file in ZXBS folder
        /// </summary>
        /// <param name="exePath">ZXBS path</param>
        /// <returns>Version info</returns>
        private static ExternalTools_Version GetZXBSVersion(string exePath)
        {
            try
            {
                var fileName = Path.Combine(exePath, "version.txt");
                if (!File.Exists(fileName))
                {
                    // no version.txt file
                    if (File.Exists(Path.Combine(exePath, "ZXBasicStudio.exe"))
                        || File.Exists(Path.Combine(exePath, "ZXBasicStudio")))
                    {
                        // return "OLD version"
                        return new ExternalTools_Version()
                        {
                            DownloadUrl = "",
                            BetaNumber = 0,
                            OperatingSystem = OperatingSystems.All,
                            Version = "OLD version",
                            VersionNumber = 0
                        };
                    }
                    return null;
                }
                // Read version from file
                var txt = File.ReadAllText(fileName);
                var v = GetVersionNumber(txt);

                // Is an stable version (ends with .0)
                var parts = txt.Split(".");
                if (parts.Length == 4 && parts[3] == "0")
                {
                    txt = $"{parts[0]}.{parts[1]}.{parts[2]}";
                }

                var version = new ExternalTools_Version()
                {
                    DownloadUrl = "",
                    BetaNumber = v.Item2,
                    OperatingSystem = OperatingSystems.All,
                    Version = txt,
                    VersionNumber = v.Item1
                };
                return version;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error getting local ZXBS version.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Get version from file using --version parameter
        /// </summary>
        /// <param name="fileName">Executable filename</param>
        /// <returns>ExternalTools_Version with the version info</returns>
        private static ExternalTools_Version GetVersionFromParameter(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return null;
                }

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
                ShowMessage($"Error getting local Boriel Basic version.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }


        /// <summary>
        /// Gewt own version
        /// </summary>
        /// <param name="exePath">Not needed</param>
        /// <returns>Version info</returns>
        public static ExternalTools_Version GetZXBSInstallerVersion(string exePath)
        {
            try
            {
                // Get assembly version
                var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
                var parts = assemblyVersion.Split('.');
                if (parts.Length < 4)
                {
                    return null;
                }
                ;
                var version = $"{parts[0]}.{parts[1]}.{parts[2]}";
                // It's a beta?
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
                ShowMessage($"Error getting local ZXBSInstaller version.\r\n{ex.Message}{ex.StackTrace}");
                return null;
            }
        }

        #endregion


        #region Install external tool

        /// <summary>
        /// Download and install al selected tools
        /// </summary>
        public static void DownloadAndInstallTools()
        {
            try
            {
                ShowStatusPanel($"Working...");
                foreach (var tool in ExternalTools)
                {
                    if (Cancel)
                    {
                        break;
                    }
                    if (tool.IsSelected)
                    {
                        DownloadAndInstallTool(tool, tool.LatestVersion);
                    }
                }
                HideStatusPanel();
                RefreshTools();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error installing.\r\n{ex.Message}{ex.StackTrace}");
            }
        }


        /// <summary>
        /// Download and install one tool
        /// </summary>
        /// <param name="tool">Tool to install</param>
        /// <param name="version">Version info to install</param>
        public static void DownloadAndInstallTool(ExternalTool tool, ExternalTools_Version version)
        {
            // For debug
            string step = "";
            try
            {
                if (tool == null || version == null)
                {
                    return;
                }

                if (tool.Id == "zxbsinstaller")
                {
                    ShowStatusPanel($"After installing or updating ZXBSInstaller, run this program from {tool.LocalPath}.");
                }

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

                // ZXBSInstaller auto install
                if (tool.Id == "zxbsinstaller")
                {
                    InstallInstaller(tool, tempFile, installationPath);
                    return;
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


        /// <summary>
        /// Install ZXBSInstaller...
        /// Generate a batch/bash file to expand .zip, decompress and launch ZXBSInstaller
        /// </summary>
        /// <param name="tool">Tool info for ZXBSInstaller</param>
        /// <param name="tempFile">Local path of the downloaded .zip file with the new version of ZXBSInstaller</param>
        /// <param name="installationPath">Installation destination path</param>
        private static void InstallInstaller(ExternalTool tool, string tempFile, string installationPath)
        {
            try
            {
                // Create batch/bash file
                string bash = "";
                string bashFile = "";
                if (CurrentOperatingSystem == OperatingSystems.Windows)
                {
                    // Windows
                    bashFile = Path.Combine(GeneralConfig.BasePath, "downloads", "zxbsinstall.bat");
                    bash = @"
@echo off
echo Updating installer...
timeout /t 5 /nobreak
echo on
tar -xf ""{tempFile}"" -C ""{installationPath}""
del ""{tempFile}""
cd ""{installationPath}""
start ZXBSInstaller.exe";
                }
                else
                {
                    // Linux and Mac
                    bashFile = Path.Combine(GeneralConfig.BasePath, "downloads", "zxbsinstall.sh");
                    bash = @"
# !/bin/bash
set -e

echo ""Updating installer...""
sleep 5

ZIP_FILE=""{tempFile}""
DEST_DIR=""{installationPath}""

extract_zip() {
    if command -v unzip >/dev/null 2>&1; then
        echo ""Using unzip...""
        unzip -o ""$ZIP_FILE"" -d ""$DEST_DIR""
    elif command -v tar >/dev/null 2>&1; then
        echo ""Using tar...""
        tar -xf ""$ZIP_FILE"" -C ""$DEST_DIR""
    else
        echo ""Error: Neither unzip nor tar is installed.""
        exit 1
    fi
}

extract_zip

rm -f ""$ZIP_FILE""
cd ""$DEST_DIR"" || exit 1

# Ejecutar sin esperar
./ZXBSInstaller &
";
                }
                bash = bash.Replace("{tempFile}", tempFile).Replace("{installationPath}", installationPath);
                File.WriteAllText(bashFile, bash);

                if (CurrentOperatingSystem != OperatingSystems.Windows)
                {
                    // Set execute attr in Linux/Mac
                    {
                        var process = new Process();
                        process.StartInfo.FileName = "chmod";
                        process.StartInfo.ArgumentList.Add("+x");
                        process.StartInfo.ArgumentList.Add(bashFile);
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.UseShellExecute = false;
                        process.Start();
                        process.WaitForExit();
                    }
                    // Launch .sh file
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = bashFile,
                        WorkingDirectory = Path.Combine(GeneralConfig.BasePath, "downloads"),
                        UseShellExecute = true,
                    };
                    var p = new Process { StartInfo = psi };
                    p.Start();
                }
                else
                {
                    // Launch .bat file
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = bashFile,
                        WorkingDirectory = Path.Combine(GeneralConfig.BasePath, "downloads"),
                        UseShellExecute = true,
                    };
                    var p = new Process { StartInfo = psi };
                    p.Start();
                }

                // Exit app
                ExitApp();
            }

            catch (Exception ex)
            {
                HideStatusPanel();
                ShowMessage($"Error installing ZXBSInstaller\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }


        /// <summary>
        /// Extract file
        /// </summary>
        /// <param name="archive">Path of the .zip file</param>
        /// <param name="destination">Destination folder</param>
        private static void ExtractFile(string archive, string destination)
        {
            try
            {
                if (archive.ToLower().EndsWith(".zip"))
                {
                    // Extract .zip file
                    System.IO.Compression.ZipFile.ExtractToDirectory(archive, destination, true);
                }
                else if (CurrentOperatingSystem != OperatingSystems.Windows)
                {
                    // Extract .tar file on Linux and Mac
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
            }
            catch (Exception ex)
            {
                ShowMessage($"Error unpacking file {archive} on {destination}.\r\n{ex.Message}{ex.StackTrace}");
            }
        }


        /// <summary>
        /// Update ZX Basic Studio config with the tools path for "zxbc" and "zxbasm"
        /// </summary>
        private static void SetZXBSConfig()
        {
            try
            {
                // Build path for ZX Basic Studio configuration file
                var filePath = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "ZXBasicStudio", "ZXBasicStudioOptions.json");
                if (!File.Exists(filePath))
                {
                    // Create a default config if not exists
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

                // Open config file
                var sb = new StringBuilder();
                var sr = new StreamReader(filePath);
                while (!sr.EndOfStream)
                {
                    // Read one line
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
                ShowMessage($"Error updating ZX Basic Studio options.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }


        #endregion


        /// <summary>
        /// Launch ZX Basic Studio
        /// </summary>
        /// <returns>True if correct or false if error</returns>
        public static bool RunZXBasicStudio()
        {
            try
            {
                var fileName = Path.Combine(GeneralConfig.BasePath, "zxbs", "ZXBasicStudio.exe");
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(GeneralConfig.BasePath, "zxbs", "ZXBasicStudio");
                }
                if (!File.Exists(fileName))
                {
                    ServiceLayer.ShowMessage("ZX Basic Studio executable not found. Please check the installation.");
                    return false;
                }
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                };
                using Process process = new Process { StartInfo = psi };
                process.Start();
                return true;
            }
            catch (Exception ex)
            {
                ServiceLayer.ShowMessage($"Error launching ZX Basic Studio. Please check the installation.\r\n{ex.Message}{ex.StackTrace}");
                return false;
            }
        }
    }
}