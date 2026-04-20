using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Neg
{
    public class ZXMapsTiles
    {
        public string Name { get; set; }
        public GraphicsModes GraphicMode { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public List<ZXMapsTile> Tiles { get; set; }
    }
}
