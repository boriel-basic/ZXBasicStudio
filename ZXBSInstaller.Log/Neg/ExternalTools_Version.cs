namespace ZXBSInstaller.Log.Neg
{
    /// <summary>
    /// Download data and version info for external tool
    /// </summary>
    public class ExternalTools_Version
    {
        /// <summary>
        /// Display version for this version
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Numer of the Beta version, 0 if not a beta
        /// </summary>
        public int BetaNumber { get; set; }
        /// <summary>
        /// Internal version number to order versions
        /// </summary>
        public int VersionNumber { get; set; }
        /// <summary>
        /// Download url for this version
        /// </summary>
        public string DownloadUrl { get; set; }
        /// <summary>
        /// Operating system for this version
        /// </summary>
        public OperatingSystems OperatingSystem { get; set; }
    }
}