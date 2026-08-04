using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using System.Xml.Schema;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;
using ZXBasicStudio.Extensions;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class MapPatternEditor : UserControl
    {
        #region Private properties

        /// <summary>
        /// last zoom value
        /// </summary>
        private int lastZoom = 0;

        /// <summary>
        /// Zooms values
        /// </summary>
        private int[] zooms = new int[]
        {
            1,2,4,8,16,24,32,48,64
        };
        private int pixelSize = 72;

        private byte actualFrame = 0;

        #endregion


        #region Public properties

        /// <summary>
        /// Zoom
        /// </summary>
        public int Zoom
        {
            get
            {
                return _Zoom;
            }
            set
            {
                _Zoom = value;
                Refresh(false);
            }
        }


        /// <summary>
        /// Color index of the primary color
        /// </summary>
        public int PrimaryColorIndex { get; set; }

        /// <summary>
        /// Color index of the secondary color
        /// </summary>
        public int SecondaryColorIndex { get; set; }

        public bool Bright { get; set; }
        public bool Flash { get; set; }

        private bool _ViewAttributes = true;

        public bool InvertPixelsCell { get; set; } = false;

        public bool InvertColorsCell { get; set; } = false;

        public bool ColorPicker { get; set; } = false;

        public ZXMapsMap Map
        {
            get
            {
                return _map;
            }
            set
            {
                _map = value;
                Refresh(false);
            }
        }

        public ZXMapsTiles Tiles
        {
            get
            {
                return _tiles;
            }
            set
            {
                _tiles = value;
                Refresh(false);
            }
        }

        public int CurrentTile { get; set; }

        #endregion


        #region Private fields

        private ZXMapsMap _map = null;
        private ZXMapsTiles _tiles = null;
        private int _Zoom = 24;
        private Action<MapPatternEditor, string> CallBackCommand = null;
        private int? lastId = null;
        private ZXGridImageView[,] images = null;


        /// <summary>
        /// True when mouse left button is pressed
        /// </summary>
        private bool MouseLeftPressed = false;
        /// <summary>
        /// True when moude right button is pressed
        /// </summary>
        private bool MouseRightPressed = false;

        /// <summary>
        /// Dispatcher for refresh operations
        /// </summary>
        private DispatcherTimer tmr = null;

        #endregion


        #region Public Methods

        public MapPatternEditor()
        {
            InitializeComponent();

            PrimaryColorIndex = 1;
            SecondaryColorIndex = 0;

            cnvEditor.PointerMoved += CnvEditor_PointerMoved;
            cnvEditor.PointerPressed += CnvEditor_PointerPressed;

            sldZoom.PropertyChanged += SldZoom_PropertyChanged;
            txtFrame.PropertyChanged += TxtFrame_PropertyChanged;

            btnClear.Tapped += BtnClear_Tapped;
            btnExport.Tapped += BtnExport_Tapped;

            btnUndo.Tapped += BtnUndo_Tapped;
            btnRedo.Tapped += BtnRedo_Tapped;

            btnPaper.Tapped += BtnPaper_Click;
            btnInk.Tapped += BtnInk_Tapped;
            UpdateColorPanel();
        }


        /// <summary>
        /// Initializes the control
        /// </summary>
        /// <param name="callBackCommand">CallBack for commands: "REFRESH"</param>
        /// <returns>True if OK or False if error</returns>
        public bool Initialize(ZXMapsMap map, ZXMapsTiles tileData, Action<MapPatternEditor, string> callBackCommand)
        {
            this._map = map;
            this._tiles = tileData;
            this.CallBackCommand = callBackCommand;
            Refresh(false);
            return true;
        }


        /// <summary>
        /// Refresh the draw area
        /// </summary>
        public void Refresh(bool withCallBack)
        {
            if (_map == null || _tiles == null)
            {
                cnvEditor.Children.Clear();
                return;
            }

            if (images == null)
            {
                images = new ZXGridImageView[_map.Width, _map.Height];
                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        var i = new ZXGridImageView();
                        i.Show8x8Grid = false;
                        i.ViewAttributes = false;
                        //i.PointerEntered += I_PointerEntered;
                        //i.PointerExited += I_PointerExited;
                        //i.PointerPressed += I_PointerPressed;
                        //i.PointerReleased += I_PointerReleased;
                        //i.PointerMoved += I_PointerMoved;
                        //i.Tag = x + (y * _map.Width);
                        images[x, y] = i;
                        cnvEditor.Children.Add(i);
                    }
                }
            }

            if (_map.TileArrays == null)
            {
                _map.TileArrays = new ZXMapsTileArray[_map.Width, _map.Height];
                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        _map.TileArrays[x, y] = new ZXMapsTileArray()
                        {
                            Id = x + (y * _map.Width),
                            Properties = new List<ZXMapsProperty>(),
                            TileId = 0,
                            X = x,
                            Y = y,
                            Z = 0
                        };
                    }
                }
            }

            for (int y = 0; y < _map.Height; y++)
            {
                int yy = y * _Zoom * 4;
                for (int x = 0; x < _map.Width; x++)
                {
                    var i = images[x, y];
                    i.Zoom = _Zoom;
                    Canvas.SetLeft(i, x * i.Width);
                    Canvas.SetTop(i, y * i.Height);
                    if (x < _map.TileArrays.GetLength(0) &&
                        y < _map.TileArrays.GetLength(1))
                    {
                        var td = _map.TileArrays[x, y];
                        if (td != null)
                        {
                            var tile = _tiles.Tiles.FirstOrDefault(d => d != null && d.Id == td.TileId);
                            var aspect = new TileImage();
                            aspect.RenderTile(tile, 0);
                            i.BackgroundImage = aspect;
                        }
                    }
                }
            }

            cnvEditor.Width = images[0, 0].Width * _map.Width;
            cnvEditor.Height = images[0, 0].Height * _map.Height;

            this.InvalidateVisual();

            if (withCallBack)
            {
                CallBackCommand?.Invoke(this, "REFRESH");
            }
        }

        #endregion


        #region Undo and Redo

        private List<ZXMapsTileArray> operations = new List<ZXMapsTileArray>();
        private int operationIndex = -1;

        public void Undo()
        {
            if (operationIndex > operations.Count - 1)
            {
                operationIndex = operations.Count - 1;
            }
            if (operationIndex < 0)
            {
                return;
            }
            var op = operations[operationIndex];
            if (op == null)
            {
                return;
            }
            _map.TileArrays[op.X, op.Y] = op;
            operationIndex--;
            Refresh(true);
        }


        public void Redo()
        {
            if (operationIndex > operations.Count - 1)
            {
                return;
            }
            if (operationIndex < 0)
            {
                return;
            }
            var op = operations[operationIndex];
            _map.TileArrays[op.X, op.Y] = op;
            operationIndex++;
            Refresh(true);
        }

        #endregion


        #region Drawing events

        /// <summary>
        /// Event for mouse pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CnvEditor_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var p = e.GetCurrentPoint(cnvEditor);
            var w = images[0, 0].Width;
            var h = images[0, 0].Height;
            int x = (int)(p.Position.X / w);
            int y = (int)(p.Position.Y / h);

            if (p.Properties.IsLeftButtonPressed)
            {
                SetTile(x, y, CurrentTile);
            }
            if (p.Properties.IsRightButtonPressed)
            {
                SetTile(x, y, 0);
            }
        }


        /// <summary>
        /// Event for pointer moved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CnvEditor_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (e.Properties.IsLeftButtonPressed || e.Properties.IsRightButtonPressed)
            {
                var p = e.GetCurrentPoint(cnvEditor);
                var w = images[0, 0].Width;
                var h = images[0, 0].Height;
                int x = (int)(p.Position.X / w);
                int y = (int)(p.Position.Y / h);

                if (e.Properties.IsLeftButtonPressed)
                {
                    SetTile(x, y, CurrentTile);
                }
                else if (e.Properties.IsRightButtonPressed)
                {
                    SetTile(x, y, 0);
                }
            }
        }


        private int lastX = -1;
        private int lastY = -1;
        private int lastValue = -1;

        /// <summary>
        /// Set point to a color value
        /// </summary>
        /// <param name="mx">Absolute x</param>
        /// <param name="my">Absolute y</param>
        /// <param name="value">Value of the point</param>
        private void SetTile(int x, int y, int value)
        {
            if (_map == null)
            {
                return;
            }

            if(lastX==x && lastY==y && lastValue == value)
            {
                return;
            }
            var ta = _map.TileArrays[x, y];
            operations.Add(ta.Clonar<ZXMapsTileArray>());
            operationIndex = operations.Count - 1;
            lastX = ta.X;
            lastY = ta.Y;
            lastValue = value;

            if (ta.TileId != value)
            {
                ta.TileId = value;
                Refresh(true);
            }
        }


        private void Tmr_Tick(object? sender, EventArgs e)
        {
            Refresh(true);
            tmr.Stop();
        }

        #endregion


        #region Icon actions


        /// <summary>
        /// Clear Tile
        /// </summary>
        public void Clear()
        {
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    SetTile(x, y, 0);
                }
            }
            Refresh(true);
        }

        #endregion


        #region Color

        private void BtnPaper_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //if (ctrlColorPicker.IsVisible)
            //{
            //    ctrlColorPicker.IsVisible = false;
            //    return;
            //}
            //var Tile = TileData;
            //if (Tile.GraphicMode == GraphicsModes.Monochrome)
            //{
            //    return;
            //}
            //ctrlColorPicker.IsVisible = true;
            //ctrlColorPicker.Inicialize(Tile.GraphicMode, Tile.Palette, SecondaryColorIndex, ColorPickerPaper_Action);
        }


        private void BtnInk_Tapped(object? sender, TappedEventArgs e)
        {
            //if (ctrlColorPicker.IsVisible)
            //{
            //    ctrlColorPicker.IsVisible = false;
            //    return;
            //}
            //var Tile = TileData;
            //if (Tile.GraphicMode == GraphicsModes.Monochrome)
            //{
            //    return;
            //}
            //ctrlColorPicker.IsVisible = true;
            //ctrlColorPicker.Inicialize(Tile.GraphicMode, Tile.Palette, PrimaryColorIndex, ColorPickerInk_Action);
        }


        public void UpdateColorPanel()
        {
            //var Tile = TileData;
            //if (Tile == null)
            //{
            //    return;
            //}
            //switch (Tile.GraphicMode)
            //{
            //    case GraphicsModes.Monochrome:
            //        {
            //            var ink = Tile.Palette[1];
            //            var paper = Tile.Palette[0];
            //            grdPaper.Background = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
            //            txtPaper.Foreground = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
            //            txtPaper.Text = "0";
            //            grdInk.Background = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
            //            txtInk.Foreground = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
            //            txtInk.Text = "1";
            //        }
            //        break;

            //    case GraphicsModes.ZXSpectrum:
            //    case GraphicsModes.Next:
            //        {
            //            var ink = Tile.Palette[PrimaryColorIndex];
            //            var paper = Tile.Palette[SecondaryColorIndex];
            //            grdPaper.Background = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
            //            txtPaper.Foreground = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
            //            txtPaper.Text = SecondaryColorIndex.ToString();
            //            grdInk.Background = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
            //            txtInk.Foreground = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
            //            txtInk.Text = PrimaryColorIndex.ToString();
            //        }
            //        break;
            //}
        }



        private void ColorPickerPaper_Action(string command, int indexColor)
        {
            SecondaryColorIndex = indexColor;
            UpdateColorPanel();
        }


        private void ColorPickerInk_Action(string command, int indexColor)
        {
            PrimaryColorIndex = indexColor;
            UpdateColorPanel();
        }

        #endregion



        #region Main editor

        /// <summary>
        /// Zoom changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SldZoom_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            int z = (int)sldZoom.Value;
            if (z == 0 || z == lastZoom)
            {
                return;
            }
            lastZoom = z;

            z = zooms[z - 1];
            pixelSize = z * 4;
            txtZoom.Text = "Zoom " + z.ToString() + "x";
            Zoom = z;
        }


        public void ZoomIn()
        {
            int v = sldZoom.Value.ToInteger();
            if (v < zooms.Length - 1)
            {
                sldZoom.Value = v - 1;
            }
        }


        public void ZoomOut()
        {
            int v = sldZoom.Value.ToInteger();
            if (v > 0)
            {
                sldZoom.Value = v - 1;
            }

        }


        private void TxtFrame_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            // TODO: Multiple frames per tile?
            /*
            byte v = txtFrame.Text.ToByte();
            if (actualFrame == v ||
                ctrlProperties.TileData == null ||
                v < 0 ||
                v >= (ctrlProperties.TileData.Frames))
            {
                return;
            }
            if (v > 255)
            {
                v = 255;
            }
            actualFrame = v;
            if (TileData != null)
            {
                TileData.CurrentFrame = actualFrame;
                Refresh();
            }
            Refresh();
            */
        }

        #endregion


        #region ToolBar


        private void BtnRedo_Tapped(object? sender, TappedEventArgs e)
        {
            Redo();
        }


        private void BtnUndo_Tapped(object? sender, TappedEventArgs e)
        {
            Undo();
        }


        /// <summary>
        /// Clear click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClear_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.Clear();
        }


        private void BtnExport_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Export();
        }


        public void Export()
        {
            /*
            var dlg = new TileExportDialog();
            dlg.Initialize(FileName, TilePatternsList.Select(d => d.TileData));
            dlg.ShowDialog(this.VisualRoot as Window);
            */
        }

        #endregion        
    }
}
