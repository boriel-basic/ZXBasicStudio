using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;

//using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.NextDows.neg;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;
using ZXBasicStudio.DocumentEditors.ZXTextEditor.Classes.Folding;
using ZXBasicStudio.DocumentModel.Classes;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class MapPropertiesControl : UserControl
    {
        private ZXMapsMap Map = null;
        private int CurrentTap = 0;


        public MapPropertiesControl()
        {
            InitializeComponent();
        }


        public bool Initialize(ZXMapsMap map)
        {
            this.Map = map;
            Refresh();
            return true;
        }        


        public void Refresh()
        {            
            // Map
            cmbMapType.SelectedIndex = (int)Map.MapType;
            txtMapHeight.Value = Map.Height;
            txtMapWidth.Value = Map.Width;
            txtMapTileHeight.Value = Map.TileHeight;
            txtMapTileWidth.Value = Map.TileWidth;
            cmbMappingType.SelectedIndex = (int)Map.MappingType;
            // Layer
            var layer = Map.Layers.FirstOrDefault(d => d.Selected);
            if (layer == null)
            {
                tabControl.SelectedIndex = 0;
            }
            else
            {
                txtLayerName.Text = layer.Name;
                cmbLayerType.SelectedIndex = (int)layer.LayersType;
            }
        }
    }
}