using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Neg
{
    /// <summary>
    /// Represents a map layer within the ZX Maps system, providing properties to control its visibility, rendering
    /// order, and data type.
    /// </summary>
    /// <remarks>Use this class to manage the display and organization of individual layers in a ZX Maps
    /// document. The layer's type, order, and visibility can be customized to control how map data is presented and
    /// interacted with. The specific kind of data represented by the layer is determined by the LayersType
    /// property.</remarks>
    public class ZXMapsLayer
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the entity.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the order in which the item appears in a collection.
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the associated element is visible.
        /// </summary>
        public bool Visible { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the item is selected.
        /// </summary>
        public bool Selected { get; set; }
        /// <summary>
        /// Gets or sets the type of map layers to display.
        /// </summary>
         public ZXMapsLayersType LayersType { get; set; }
    }
}
