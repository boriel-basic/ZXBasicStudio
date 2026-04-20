using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using System.Xml.Schema;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.Extensions;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class TilePatternEditor : UserControl
    {
        #region Public properties

        /// <summary>
        /// Actual Tile data
        /// </summary>
        public ZXMapsTile TileData
        {
            get
            {
                return _TileData;
            }
            set
            {
                _TileData = value;
                if (_TileData == null)
                {
                    lastId = null;
                }
                else
                {
                    if (lastId != _TileData.Id)
                    {
                        _TileData.CurrentFrame = 0;
                        lastId = _TileData.Id;
                    }
                }
                Undo_AddPoint();
                Refresh(true);
            }
        }


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
                Refresh(true);
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

        public bool ViewAttributes
        {
            get
            {
                return _ViewAttributes;
            }
            set
            {
                _ViewAttributes = value;
                aspect.ViewAttributes = value;
                Refresh(false);
            }
        } 

        private bool _ViewAttributes = true;

        public bool InvertPixelsCell { get; set; } = false;

        public bool InvertColorsCell { get; set; } = false;

        public bool ColorPicker { get; set; } = false;

        #endregion


        #region Private fields

        private ZXMapsTile _TileData = null;
        private int _Zoom = 24;
        private Action<TilePatternEditor, string> CallBackCommand = null;
        private int? lastId = null;
        private TileImage aspect = new TileImage();

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

        public TilePatternEditor()
        {
            InitializeComponent();

            grdEditor.BackgroundImage = aspect;

            PrimaryColorIndex = 1;
            SecondaryColorIndex = 0;

            grdEditor.PointerMoved += GrdEditor_PointerMoved;
            grdEditor.PointerPressed += GrdEditor_PointerPressed;
            grdEditor.PointerReleased += GrdEditor_PointerReleased;
            grdEditor.PointerExited += GrdEditor_PointerExited;

            aspect.ViewAttributes = true;
        }


        /// <summary>
        /// Initializes the control
        /// </summary>
        /// <param name="callBackCommand">CallBack for commands: "REFRESH"</param>
        /// <returns>True if OK or False if error</returns>
        public bool Initialize(Action<TilePatternEditor, string> callBackCommand)
        {
            this.CallBackCommand = callBackCommand;
            return true;
        }


        /// <summary>
        /// Refresh the draw area
        /// </summary>
        public void Refresh(bool callBack = false)
        {
            //cnvEditor.Children.Clear();
            if (TileData == null)
            {
                aspect.Clear(Colors.White);
                grdEditor.InvalidateVisual();
                return;
            }


            aspect.RenderTile(TileData, TileData.CurrentFrame);
            grdEditor.Zoom = _Zoom;
            this.InvalidateVisual();

            if (callBack)
            {
                CallBackCommand?.Invoke(this, "REFRESH");
            }
        }

        #endregion


        #region Undo and Redo

        private List<Pattern> operations = new List<Pattern>();
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
            if (Pattern_Equals(TileData.Patterns[TileData.CurrentFrame], op))
            {
                operationIndex--;
                Undo();
                return;
            }
            if (op != null)
            {
                TileData.Patterns[TileData.CurrentFrame] = op;
                Refresh();
            }
            CallBackCommand(this, "REFRESH");
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
            if (Pattern_Equals(TileData.Patterns[TileData.CurrentFrame], op))
            {
                operationIndex++;
                Redo();
                return;
            }
            if (op != null)
            {
                TileData.Patterns[TileData.CurrentFrame] = op;
                Refresh();
            }
        }


        private void Undo_AddPoint()
        {
            try
            {
                if (TileData == null || TileData.Patterns == null)
                {
                    operations.Clear();
                    return;
                }

                if (operationIndex >= 0 &&
                    operationIndex < operations.Count)
                {
                    var lastOp = operations[operationIndex];
                    if (Pattern_Equals(TileData.Patterns[TileData.CurrentFrame], lastOp))
                    {
                        return; // No changes
                    }
                }
                operations = operations.Take(operationIndex+1).ToList();

                var op = TileData.Patterns[TileData.CurrentFrame].Clonar<Pattern>();
                operations.Add(op);
                operationIndex = operations.Count - 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Undo_AddPoint: {ex.Message}");
            }
        }

        private bool Pattern_Equals(Pattern p1, Pattern p2)
        {
            if (p1 == null || p2 == null)
            {
                return false;
            }
            var pc1 = p1.Serializar();
            var pc2 = p2.Serializar();
            return pc1 == pc2;
        }
        #endregion


        #region Drawing events

        /// <summary>
        /// Event for mouse pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrdEditor_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var p = e.GetCurrentPoint(grdEditor);

            if (ColorPicker)
            {
                int x = (int)p.Position.X;
                int y = (int)p.Position.Y;
                x = x / (_Zoom + 1);
                y = y / (_Zoom + 1);
                var atr=GetAttribute(TileData.Patterns[TileData.CurrentFrame], x,y);
                PrimaryColorIndex = atr.Ink;
                SecondaryColorIndex = atr.Paper;
                ColorPicker = false;
                Refresh(true);
            }
            else if (InvertPixelsCell)
            {
                GrdEditor_InvertPixelsCell(p.Position.X, p.Position.Y);
            }
            else if (InvertColorsCell)
            {
                GrdEditor_InvertColorsCell(p.Position.X, p.Position.Y);
            }
            else
            {
                if (p.Properties.IsLeftButtonPressed)
                {
                    SetPoint(p.Position.X, p.Position.Y, PrimaryColorIndex);
                    MouseLeftPressed = true;
                    MouseRightPressed = false;
                }
                else if (p.Properties.IsRightButtonPressed)
                {
                    SetPoint(p.Position.X, p.Position.Y, SecondaryColorIndex);
                    MouseLeftPressed = false;
                    MouseRightPressed = true;
                }
            }
        }        


        /// <summary>
        /// Event for mouse released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrdEditor_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            MouseLeftPressed = false;
            MouseRightPressed = false;
        }


        /// <summary>
        /// Event for pointer moved outside control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrdEditor_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            MouseLeftPressed = false;
            MouseRightPressed = false;
        }


        /// <summary>
        /// Event for pointer moved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrdEditor_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            var p = e.GetCurrentPoint(grdEditor);
            if (MouseLeftPressed)
            {
                SetPoint(p.Position.X, p.Position.Y, PrimaryColorIndex);
            }
            else if (MouseRightPressed)
            {
                SetPoint(p.Position.X, p.Position.Y, SecondaryColorIndex);
            }
        }


        /// <summary>
        /// Set point to a color value
        /// </summary>
        /// <param name="mx">Absolute x</param>
        /// <param name="my">Absolute y</param>
        /// <param name="value">Value of the point</param>
        private void SetPoint(double mx, double my, int value)
        {
            if (TileData == null)
            {
                return;
            }

            int x = (int)mx;
            int y = (int)my;

            x = x / (_Zoom + 1);
            y = y / (_Zoom + 1);

            if (x < 0 || y < 0 || x >= TileData.Width || y >= TileData.Height)
            {
                return;
            }

            int dir = (TileData.Width * y) + x;
            var Tile = TileData.Patterns[TileData.CurrentFrame];

            switch (TileData.GraphicMode)
            {
                case GraphicsModes.Monochrome:
                    Tile.RawData[dir] = value;
                    break;
                case GraphicsModes.ZXSpectrum:
                    {
                        if (value == PrimaryColorIndex)
                        {
                            Tile.RawData[dir] = 1;
                        }
                        else
                        {
                            Tile.RawData[dir] = 0;
                        }
                        SetAttribute(Tile, x, y);
                    }
                    break;
            }

            Undo_AddPoint();
            Refresh(false);

            if (tmr == null)
            {
                tmr = new DispatcherTimer();
                tmr.Tick += Tmr_Tick;
                tmr.Interval = TimeSpan.FromMilliseconds(250);
            }
            tmr.Stop();
            tmr.Start();
        }


        private void SetAttribute(Pattern pattern, int x, int y)
        {
            int cW = TileData.Width / 8;
            int cX = x / 8;
            int cY = y / 8;
            var attr = pattern.Attributes[(cY * cW) + cX];
            attr.Ink = PrimaryColorIndex;
            attr.Paper = SecondaryColorIndex;
            attr.Flash = false;
            switch (TileData.GraphicMode)
            {
                case GraphicsModes.Monochrome:
                    attr.Bright = false;
                    break;
                case GraphicsModes.ZXSpectrum:
                    if (PrimaryColorIndex > 7 || SecondaryColorIndex > 7)
                    {
                        attr.Bright = true;
                    }
                    else
                    {
                        attr.Bright = false;
                    }
                    break;
            }
            pattern.Attributes[(cY * cW) + cX] = attr;
        }


        private AttributeColor GetAttribute(Pattern pattern, int x, int y)
        {
            if(pattern.Attributes == null)
            {
                pattern.Attributes = new AttributeColor[(TileData.Width / 8) * (TileData.Height / 8)];
            }
            int cW = TileData.Width / 8;
            int cX = x / 8;
            int cY = y / 8;
            return pattern.Attributes[(cY * cW) + cX];
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
            if (TileData == null || TileData.Patterns == null ||
                TileData.CurrentFrame >= (TileData.Patterns.Count) ||
                TileData.Patterns[TileData.CurrentFrame].RawData == null)
            {
                return;
            }
            for (int n = 0; n < TileData.Patterns[TileData.CurrentFrame].RawData.Length; n++)
            {
                TileData.Patterns[TileData.CurrentFrame].RawData[n] = SecondaryColorIndex;
            }
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Cut patterns to clipboard
        /// </summary>
        public void Cut()
        {
            Undo_AddPoint();
            Copy();
            Clear();
        }


        /// <summary>
        /// Copy patterns to clipboard
        /// </summary>
        public void Copy()
        {
            var patterns = new Pattern[1] { TileData.Patterns[TileData.CurrentFrame] };
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(patterns.Serializar()).Wait();
        }


        /// <summary>
        /// Paste patterns from clipboard
        /// </summary>
        public async void Paste()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var cbData = await TopLevel.GetTopLevel(this).Clipboard.GetTextAsync();
            if (string.IsNullOrEmpty(cbData))
            {
                return;
            }
            var cbPatterns = cbData.Deserializar<Pattern[]>();
            if (cbPatterns == null)
            {
                return;
            }

            if (cbPatterns.Length == 1)
            {
                if (cbPatterns[0].RawData == null)
                {
                    TileData.Patterns[TileData.CurrentFrame].RawData = ServiceLayer.PointData2RawData(cbPatterns[0].Data, 8, 8);
                }
                else
                {
                    TileData.Patterns[TileData.CurrentFrame].RawData = cbPatterns[0].RawData;
                    TileData.Patterns[TileData.CurrentFrame].Attributes = cbPatterns[0].Attributes;
                }
            }
            else
            {
                // Create an empty pattern
                var pat2 = pattern.Clonar<Pattern>();
                pat2.RawData = new int[TileData.Width * TileData.Height];

                if (cbPatterns[0].RawData == null)
                {
                    // Paste from PointData
                    int ox = 0;
                    int oy = 0;
                    int dir = 0;
                    for (int n = 0; n < cbPatterns.Length; n++)
                    {
                        //int d1 = (n / TileData.Width);
                        //int d2 = (d1 * TileData.Width) - n;
                        for (int py = 0; py < 8; py++)
                        {
                            for (int px = 0; px < 8; px++)
                            {
                                var po = cbPatterns[n].Data.FirstOrDefault(d => d.X == px && d.Y == py);
                                if (po != null)
                                {
                                    dir = ((oy + py) * TileData.Width) + (ox + px);

                                    if (dir < pattern.RawData.Length)
                                    {
                                        TileData.Patterns[TileData.CurrentFrame].RawData[dir] = po.ColorIndex;
                                        //pattern.RawData[dir] = po.ColorIndex;
                                    }
                                }
                            }
                        }
                        ox += 8;
                        if (ox >= TileData.Width)
                        {
                            ox = 0;
                            oy += 8;
                        }
                    }
                    //TileData.Patterns[TileData.CurrentFrame].Data = pat2.Data;
                }
                else
                {
                    // Paste from RawData
                    var dat = TileData.Patterns[TileData.CurrentFrame].RawData;
                    var cbDat = cbPatterns[0].RawData;
                    for (int n = 0; n < cbDat.Length && n < dat.Length; n++)
                    {
                        dat[n] = cbDat[n];
                    }
                }
            }
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Horizontal mirror of selected patterns
        /// </summary>
        public void HorizontalMirror()
        {
            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            var pat1 = TileData.Patterns[TileData.CurrentFrame];
            var pat2 = pat1.Clonar<Pattern>();
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pat1);
                    SetPointValue(maxWidth - x - 1, y, pd1, ref pat2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pat2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Vertical mirror of selected patterns
        /// </summary>
        public void VerticalMirror()
        {
            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            var pat1 = TileData.Patterns[TileData.CurrentFrame];
            var pat2 = pat1.Clonar<Pattern>();
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pat1);
                    SetPointValue(x, maxHeight - y - 1, pd1, ref pat2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pat2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Rotate left of selected patterns
        /// </summary>
        public void RotateLeft()
        {
            if (TileData.Width != TileData.Height)
            {
                Window.GetTopLevel(this)?.ShowError("Can't do this!", "Only square graphics can be rotated (with the same width as height).");
                return;
            }

            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    SetPointValue(y, maxWidth - x - 1, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Rotate right of selected patterns
        /// </summary>
        public void RotateRight()
        {
            if (TileData.Width != TileData.Height)
            {
                Window.GetTopLevel(this)?.ShowError("Can't do this!", "Only square graphics can be rotated (with the same width as height).");
                return;
            }

            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    SetPointValue(maxHeight - y - 1, x, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move up and put the disappearing pixels at the bottom.
        /// </summary>
        public void ShiftUp()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int y2 = y - 1;
                    if (y2 < 0)
                    {
                        y2 = maxHeight - 1;
                    }
                    SetPointValue(x, y2, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move right and put the disappearing pixels at the left.
        /// </summary>
        public void ShiftRight()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int x2 = x + 1;
                    if (x2 >= maxWidth)
                    {
                        x2 = 0;
                    }
                    SetPointValue(x2, y, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move down and put the disappearing pixels at the top.
        /// </summary>
        public void ShiftDown()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int y2 = y + 1;
                    if (y2 >= maxHeight)
                    {
                        y2 = 0;
                    }
                    SetPointValue(x, y2, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move left and put the disappearing pixels at the right.
        /// </summary>
        public void ShiftLeft()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int x2 = x - 1;
                    if (x2 < 0)
                    {
                        x2 = maxWidth - 1;
                    }
                    SetPointValue(x2, y, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move up
        /// </summary>
        public void MoveUp()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int y2 = y - 1;
                    if (y2 < 0)
                    {
                        y2 = maxHeight - 1;
                        pd1 = 0;
                    }
                    SetPointValue(x, y2, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move rigth
        /// </summary>
        public void MoveRight()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int x2 = x + 1;
                    if (x2 >= maxWidth)
                    {
                        x2 = 0;
                        pd1 = 0;
                    }
                    SetPointValue(x2, y, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move down
        /// </summary>
        public void MoveDown()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int y2 = y + 1;
                    if (y2 >= maxHeight)
                    {
                        y2 = 0;
                        pd1 = 0;
                    }
                    SetPointValue(x, y2, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Move left
        /// </summary>
        public void MoveLeft()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    int x2 = x - 1;
                    if (x2 < 0)
                    {
                        x2 = maxWidth - 1;
                        pd1 = 0;
                    }
                    SetPointValue(x2, y, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Invert pixels
        /// </summary>
        public void Invert()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            var pattern2 = pattern.Clonar<Pattern>();

            int maxWidth = TileData.Width;
            int maxHeight = TileData.Height;
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    var pd1 = GetPointValue(x, y, pattern);
                    if (pd1 == PrimaryColorIndex)
                    {
                        pd1 = SecondaryColorIndex;
                    }
                    else if (pd1 == SecondaryColorIndex)
                    {
                        pd1 = PrimaryColorIndex;
                    }
                    else
                    {

                    }
                    SetPointValue(x, y, pd1, ref pattern2);
                }
            }
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Create a mask
        /// </summary>
        public void Mask()
        {
            var pattern = TileData.Patterns[TileData.CurrentFrame];
            int maxX = (TileData.Width) - 1;
            int maxY = (TileData.Height) - 1;

            var pattern2 = new Pattern()
            {
                Id = pattern.Id,
                Name = pattern.Name,
                Number = pattern.Number,
                RawData = new int[TileData.Width * TileData.Height]
            };

            _Mask(0, 0, ref pattern, ref pattern2);
            _Mask(maxX, 0, ref pattern, ref pattern2);
            _Mask(0, maxY, ref pattern, ref pattern2);
            _Mask(maxX, maxY, ref pattern, ref pattern2);
            TileData.Patterns[TileData.CurrentFrame] = pattern2;
            Undo_AddPoint();
            Refresh(true);
        }


        /// <summary>
        /// Create a mask (fills pattern2 from pattern1 data) called recursively
        /// </summary>
        /// <param name="x">Coord x</param>
        /// <param name="y">Coord Y</param>
        /// <param name="pattern1">Original patterns</param>
        /// <param name="pattern2">Void pattens</param>
        private void _Mask(int x, int y, ref Pattern pattern1, ref Pattern pattern2)
        {
            var p1 = GetPointValue(x, y, pattern1);
            if (p1 != 0)
            {
                return;
            }
            var p2 = GetPointValue(x, y, pattern2);
            if (p2 != 0)
            {
                return;
            }
            p2 = 1;
            SetPointValue(x, y, p2, ref pattern2);

            if (x > 1)
            {
                _Mask(x - 1, y, ref pattern1, ref pattern2);
            }
            if (x < (TileData.Width) - 1)
            {
                _Mask(x + 1, y, ref pattern1, ref pattern2);
            }
            if (y > 1)
            {
                _Mask(x, y - 1, ref pattern1, ref pattern2);
            }
            if (y < (TileData.Height) - 1)
            {
                _Mask(x, y + 1, ref pattern1, ref pattern2);
            }
        }


        /// <summary>
        /// Get a point (pixel) from the selected pattern
        /// </summary>
        /// <param name="x">X coord</param>
        /// <param name="y">Y coord</param>
        /// <param name="pattern">Pattern to use</param>
        /// <returns>Point data or null if no data</returns>
        private int GetPointValue(int x, int y, Pattern pattern)
        {
            if (x < 0 || y < 0 || x > (TileData.Width - 1) || y > (TileData.Height - 1))
            {
                return SecondaryColorIndex;
            }
            return pattern.RawData[(TileData.Width * y) + x];
        }


        /// <summary>
        /// Set a point (pixel) from the selected pattern
        /// </summary>
        /// <param name="x">X coord</param>
        /// <param name="y">Y coord</param>
        /// <param name="colorIndex">Color index to set</param>
        /// <param name="pattern">Pattern to use</param>
        private void SetPointValue(int x, int y, int colorIndex, ref Pattern pattern)
        {
            if (x < 0 || y < 0 || x > (TileData.Width - 1) || y > (TileData.Height - 1))
            {
                return;
            }
            pattern.RawData[(TileData.Width * y) + x] = colorIndex;
        }

        #endregion


        #region Invert pixels and colors

        private void GrdEditor_InvertPixelsCell(double mx, double my)
        {
            if (TileData == null)
            {
                return;
            }

            int x = (int)mx;
            int y = (int)my;

            x = x / (_Zoom + 1);
            y = y / (_Zoom + 1);

            x = x / 8;
            y = y / 8;

            if (x < 0 || y < 0 || x >= TileData.Width || y >= TileData.Height)
            {
                return;
            }

            for(int py = 0; py < 8; py++)
            {
                for (int px = 0; px < 8; px++)
                {
                    int dir = ((y * 8 + py) * TileData.Width) + (x * 8 + px);
                    if (dir < TileData.Patterns[TileData.CurrentFrame].RawData.Length)
                    {
                        var value = TileData.Patterns[TileData.CurrentFrame].RawData[dir];
                        if (value == PrimaryColorIndex)
                        {
                            TileData.Patterns[TileData.CurrentFrame].RawData[dir] = SecondaryColorIndex;
                        }
                        else if (value == SecondaryColorIndex)
                        {
                            TileData.Patterns[TileData.CurrentFrame].RawData[dir] = PrimaryColorIndex;
                        }
                    }
                }
            }
            Refresh();
        }

        private void GrdEditor_InvertColorsCell(double mx, double my)
        {
            if (TileData == null)
            {
                return;
            }
            int x = (int)mx;
            int y = (int)my;
            x = x / (_Zoom + 1);
            y = y / (_Zoom + 1);

            var inkBak = PrimaryColorIndex;
            var paperBak = SecondaryColorIndex;
            var attr=GetAttribute(TileData.Patterns[TileData.CurrentFrame], x, y);
            if (attr == null)
            {
                return;
            }
            PrimaryColorIndex = attr.Paper;
            SecondaryColorIndex = attr.Ink;
            SetAttribute(TileData.Patterns[TileData.CurrentFrame], x, y);
            PrimaryColorIndex = inkBak;
            SecondaryColorIndex=paperBak;
            Refresh();
        }
        #endregion
    }
}
