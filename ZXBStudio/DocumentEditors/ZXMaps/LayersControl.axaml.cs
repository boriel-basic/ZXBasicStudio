using Avalonia;
using Avalonia.Controls;
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
    public partial class LayersControl : UserControl
    {
        private List<ZXMapsLayer> Layers = null;
        private List<LayerItemControl> LayerItems = new List<LayerItemControl>();


        public LayersControl()
        {
            InitializeComponent();
        }


        public void AddLayer(ZXMapsLayer layer)
        {

        }


        public bool Initialize(List<ZXMapsLayer> layers)
        {
            this.Layers = layers;
            Refresh();
            return true;
        }


        public void Refresh()
        {
            pnlLayers.Children.Clear();
            LayerItems.Clear();
            bool first = true;
            foreach (ZXMapsLayer layer in Layers.OrderBy(d=>d.Order))
            {
                var li=new LayerItemControl();
                LayerItems.Add(li);
                pnlLayers.Children.Add(li);
                li.Initialize(layer);
                if (first)
                {
                    li.Selected = true;
                    first = false;
                }
                else
                {
                    li.Selected = false;
                }
            }
        }
    }
}