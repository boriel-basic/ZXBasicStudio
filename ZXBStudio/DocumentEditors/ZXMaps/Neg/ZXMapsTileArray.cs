using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Neg
{
    /// <summary>
    /// Tile reference into a map
    /// </summary>
    public class ZXMapsTileArray
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Tile id in this map position
        /// </summary>
        public int TileId { get; set; }
        /// <summary>
        /// X position in this map
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// Y position in this map
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// Z position in this map
        /// </summary>
        public int Z { get; set; }
        /// <summary>
        /// Properties for this tile in this place of the map
        /// </summary>
        public List<ZXMapsProperty> Properties { get; set; }
    }
}
