using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZXBSInstaller.Log.Neg
{
    /// <summary>
    /// External tool definition for installer and updater
    /// </summary>
    public class ExternalTool
    {
        /// <summary>
        /// Internal unique identifier
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The tools is visible/enabled
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Display name of the tool
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Author of the tool
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// Description of the tool
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Operating systems supported by the tool
        /// </summary>
        public OperatingSystems[] SupportedOperatingSystems { get; set; }
        /// <summary>
        /// Site of the tool
        /// </summary>
        public string SiteUrl { get; set; }
        /// <summary>
        /// Licence type of the tool
        /// </summary>
        public string LicenseType { get; set; }
        /// <summary>
        /// Lucence URL of the tool
        /// </summary>
        public string LicenceUrl { get; set; }
        /// <summary>
        /// Url with the versions info
        /// </summary>
        public string VersionsUrl { get; set; }
        /// <summary>
        /// Local path where the tool will be installed without file name
        /// </summary>
        public string LocalPath { get; set; }
        /// <summary>
        /// Local path where the tool will be installed with file name
        /// </summary>
        public string FullLocalPath { get; set; }
        /// <summary>
        /// If this is true, the tool will be updated from ZXBSInstaller
        /// </summary>
        public bool DirectUpdate { get; set; }
        /// <summary>
        /// Order in the list
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Recommended
        /// </summary>
        public bool Recommended { get; set; }

        /// <summary>
        /// Versions of the tool
        /// </summary>
        [JsonIgnore]
        public ExternalTools_Version[] Versions { get; set; }
        /// <summary>
        /// Version installed on local computer
        /// </summary>
        [JsonIgnore]
        public ExternalTools_Version InstalledVersion { get; set; }
        /// <summary>
        /// Latest available version
        /// </summary>
        [JsonIgnore]
        public ExternalTools_Version LatestVersion { get; set; }
        /// <summary>
        /// Need to update
        /// </summary>
        [JsonIgnore]
        public bool UpdateNeeded { get; set; }

        /// <summary>
        /// Is selected for install
        /// </summary>
        [JsonIgnore]
        public bool IsSelected { get; set; }

    }
}
