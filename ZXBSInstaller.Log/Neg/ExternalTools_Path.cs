using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBSInstaller.Log.Neg
{
    /// <summary>
    /// Defines the local path for external tools
    /// </summary>
    public class ExternalTools_Path
    {
        /// <summary>
        /// External tool unique identifier
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Local path where the tool will be installed without file name
        /// </summary>
        public string LocalPath { get; set; }
    }
}
