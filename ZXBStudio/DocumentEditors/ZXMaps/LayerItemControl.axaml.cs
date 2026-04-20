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
    public partial class LayerItemControl : UserControl
    {
        public ZXMapsLayer Layer { get; set; }

        public bool Selected
        {
            get
            {
                return _Selected;
            }
            set
            {
                _Selected = value;
                Refresh();
            }
        }
        private bool _Selected = false;


        public LayerItemControl()
        {
            InitializeComponent();
        }


        public bool Initialize(ZXMapsLayer layer)
        {
            btnShowLayer.IsVisible = layer.Visible;
            btnHideLayer.IsVisible = !layer.Visible;
            txtLayerName.Text = layer.Name;
            _Selected = layer.Selected;
            return true;
        }


        public void Refresh()
        {
            svgSelected.IsVisible = _Selected;
        }
    }
}