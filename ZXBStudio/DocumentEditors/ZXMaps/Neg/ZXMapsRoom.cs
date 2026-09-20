using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Neg
{
    public class ZXMapsRoom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int IdLayer { get; set; }
        public List<ZXMapsProperty> Properties { get; set; } = new List<ZXMapsProperty>();
    }
}
