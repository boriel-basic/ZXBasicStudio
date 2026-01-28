using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBSInstaller.Log.Neg
{
    /// <summary>
    /// Config for ZXBSInstaller
    /// </summary>
    public class Config
    {
        /// <summary>
        /// URL where to download the tools list
        /// </summary>
        public string ToolsListURL { get; set; }
        /// <summary>
        /// Base path installation for tools
        /// </summary>
        public string BasePath { get; set; }
        /// <summary>
        /// Show only stable versions (no beta)
        /// </summary>
        public bool OnlyStableVersions { get; set; }
        /// <summary>
        /// Setup ZXBS config when install/update apps
        /// </summary>
        public bool SetZXBSConfig { get; set; }
        /// <summary>
        /// Local paths for external tools
        /// </summary>
        public List<ExternalTools_Path> ExternalTools_Paths { get; set; }
    }
}
