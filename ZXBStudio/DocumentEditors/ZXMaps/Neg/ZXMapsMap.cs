using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Neg
{
    /// <summary>
    /// Represents a map
    /// </summary>
    public class ZXMapsMap
    {
        public string Name { get; set; }
        public ZXMapsTypes MapType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public ZXMapsMappingTypes MappingType { get; set; }
        public List<ZXMapsPropertyDefiniton> PropertyDefinitons { get; set; }
        public List<ZXMapsLayer> Layers { get; set; } 
    }
}
