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
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ZXBasicStudio.Classes;
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
        public ZXMapsMap Map
        {
            get
            {
                return _Map;
            }
            set
            {
                _Map = value;
                Refresh();
            }
        }
        private ZXMapsMap _Map = null;
        
        public ZXMapsTiles Tiles
        {
            get
            {
                return _Tiles;
            }
            set
            {
                _Tiles = value;
                Refresh();
            }
        }
        private ZXMapsTiles _Tiles = null;

        private Action<MapPropertiesControl, string> Command = null;


        public MapPropertiesControl()
        {
            InitializeComponent();
        }


        public bool Initialize(ZXMapsMap map, ZXMapsTiles tiles, Action<MapPropertiesControl,string> callBackCommand)
        {
            this._Map = map;
            this._Tiles = tiles;
            this.Command = callBackCommand;

            cmbMapType.SelectionChanged += CmbMapType_SelectionChanged;
            txtMapWidth.ValueChanged += TxtMapWidth_ValueChanged;
            txtMapHeight.ValueChanged += TxtMapHeight_ValueChanged;
            txtMapWidth.ValueChanged += TxtMapWidth_ValueChanged;
            txtMapTileWidth.ValueChanged += TxtMapTileWidth_ValueChanged;
            txtMapTileHeight.ValueChanged += TxtMapTileHeight_ValueChanged;
            //cmbMappingType.SelectionChanged += CmbMappingType_SelectionChanged;

            Refresh();

            return true;
        }


        public void Refresh()
        {
            if (Map == null)
            {
                tabControl.SelectedIndex = 0;
                //tabControl.IsVisible = false;
            }
            else
            {
                // Map
                cmbMapType.SelectedIndex = (int)Map.MapType;
                txtMapHeight.Value = Map.Height;
                txtMapWidth.Value = Map.Width;
                txtMapTileHeight.Value = Map.TileHeight;
                txtMapTileWidth.Value = Map.TileWidth;
                cmbMappingType.SelectedIndex = (int)Map.MappingType;
                // Layer
                var layer = Map.Layers?.FirstOrDefault(d => d.Selected);
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

            if(_Map!=null && _Tiles != null)
            {
                if (_Map.TileWidth != _Tiles.Width)
                {
                    txtMapTileWidth.Background = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    txtMapTileWidth.Background = null;
                }
                if (_Map.TileHeight != _Tiles.Height)
                {
                    txtMapTileHeight.Background = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    txtMapTileHeight.Background = null;
                }
            }
        }


        #region Values changed

        private void CmbMappingType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Map.MappingType = (ZXMapsMappingTypes)cmbMappingType.SelectedIndex;
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }


        private void TxtMapTileHeight_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            Map.TileHeight = txtMapTileHeight.Text.ToInteger();
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }


        private void TxtMapTileWidth_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            Map.TileWidth = txtMapTileWidth.Text.ToInteger();
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }


        private void TxtMapHeight_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            Map.Height = txtMapHeight.Text.ToInteger();
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }


        private void TxtMapWidth_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            Map.Width = txtMapWidth.Text.ToInteger();
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }


        private void CmbMapType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Map.MapType = (ZXMapsTypes)cmbMapType.SelectedIndex;
            Command?.Invoke(this, "UPDATE");
            Refresh();
        }

        #endregion

    }
}