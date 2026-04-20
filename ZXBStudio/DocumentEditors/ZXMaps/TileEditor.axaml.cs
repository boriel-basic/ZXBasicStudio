using Avalonia.Controls;
using System.Diagnostics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;
using System;
using System.Linq;
using ZXBasicStudio.Common;
using Avalonia;
using ZXBasicStudio.DocumentModel.Classes;
using System.IO;
using ZXBasicStudio.Classes;
using AvaloniaEdit.Folding;
using Avalonia.Threading;
using ZXBasicStudio.DocumentEditors.ZXTextEditor.Classes.Folding;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ShimSkiaSharp;
using Avalonia.Interactivity;
using Avalonia.Input;
using AvaloniaEdit;
using System.Reflection;
using FFmpeg.AutoGen;
using Avalonia.Media;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    /// <summary>
    /// Editor for GDUs and Fonts
    /// </summary>
    public partial class TileEditor : ZXDocumentEditorBase, IObserver<AvaloniaPropertyChangedEventArgs>
    {
        #region Events

        public override event EventHandler? DocumentRestored;
        public override event EventHandler? DocumentModified;
        public override event EventHandler? DocumentSaved;
        public override event EventHandler? RequestSaveDocument;

        #endregion


        #region ZXDocumentBase properties

        public override string DocumentName
        {
            get
            {
                return Path.GetFileName(FileName);
            }
        }


        public override string DocumentPath
        {
            get
            {
                return Path.GetFullPath(FileName);
            }
        }


        public override bool Modified
        {
            get
            {
                return _Modified;
            }
        }

        private bool _Modified = false;

        protected virtual AbstractFoldingStrategy? foldingStrategy { get { return null; } }

        private Guid documentTypeId = Guid.Empty;

        #endregion


        #region IObserver implementation for modified document notifications

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(AvaloniaPropertyChangedEventArgs value)
        {

        }

        #endregion


        #region Private variables

        private string FileName = "";

        private ZXMapsTiles TileMain = null;
        private int tileWidth = 16;
        private int tileHeight = 16;

        /// <summary>
        /// List of Tiles patterns controls in the set
        /// </summary>
        private List<TilePatternControl> TilePatternsList = null;

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

        private byte actualFrame = 0;

        private FoldingManager? fManager;
        private DispatcherTimer? updateFoldingsTimer;

        #endregion


        #region ZXDocumentBase functions

        public override bool SaveDocument(TextWriter OutputLog)
        {
            try
            {
                var masterList = TilePatternsList.Select(d => d.TileData).ToArray();
                var sprList = new List<ZXMapsTile>();
                foreach (var spr in masterList)
                {
                    sprList.Add(spr); //.Clonar<Tile>());
                }
                foreach (ZXMapsTile spr in sprList)
                {
                    if (spr == null)
                    {
                        continue;
                    }

                    if (spr.Frames < spr.Patterns.Count())
                    {
                        spr.Patterns = spr.Patterns.Take(spr.Frames).ToList();
                    }
                }
                TileMain.Tiles = sprList;
                var dataJSon = TileMain.Serializar();
                if (!ServiceLayer.Files_SaveFileString(FileName, dataJSon))
                {
                    return false;
                }
                ;

                _Modified = false;
                DocumentSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                OutputLog.WriteLine($"Error saving file {FileName}: {ex.Message}");
                return false;
            }
        }


        public override bool RenameDocument(string NewName, TextWriter OutputLog)
        {
            FileName = NewName;
            return true;
        }


        public override bool CloseDocument(TextWriter OutputLog, bool ForceClose)
        {
            return true;
        }


        public override void Dispose()
        {
        }


        public override void Activated()
        {
            this.Focus();
            ctrlPreview.Start();
            base.Activated();
        }


        public override void Deactivated()
        {
            ctrlPreview.Stop();
            base.Deactivated();
        }

        #endregion


        /// <summary>
        /// Constructor, without parameters is mandatory to cal Initialize
        /// </summary>
        public TileEditor()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        public TileEditor(string fileName)
        {
            InitializeComponent();
            new Thread(() => Initialize(fileName)).Start();
        }


        /// <summary>
        /// Initialize the system
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True if OK, or False if error</returns>
        public bool Initialize(string fileName)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _Initialize(fileName);
            });
            return true;
        }


        private void _Initialize(string fileName)
        {
            _Modified = true;

            ServiceLayer.Initialize();

            FileName = fileName;

            TilePatternsList = new List<TilePatternControl>();

            var data = ServiceLayer.GetFileData(fileName);
            if (data == null || data.Length == 0)
            {
                TileMain = new ZXMapsTiles()
                {
                    GraphicMode = GraphicsModes.Monochrome,
                    Height = 16,
                    Width = 16,
                    Name = Path.GetFileName(fileName).ToStringNoNull().Replace(".til", "").Replace(".zxtil", ""),
                    Tiles = new List<ZXMapsTile>()
                };
            }
            if (data != null)
            {
                var dataS = Encoding.UTF8.GetString(data);
                if (!string.IsNullOrEmpty(dataS))
                {
                    TileMain = dataS.Deserializar<ZXMapsTiles>();

                    wpTileList.ItemWidth = (TileMain.Width * 4) + 4;
                    wpTileList.ItemHeight = (TileMain.Height * 4) + 4;

                    foreach (var Tile in TileMain.Tiles)
                    {
                        // Check attributes for ZX Spectrum mode
                        if (Tile != null && Tile.Patterns != null)
                        {
                            var al = (Tile.Width / 8) * (Tile.Height / 8);
                            foreach (var pattern in Tile.Patterns)
                            {
                                if (pattern.Attributes != null)
                                {
                                    pattern.Attributes = pattern.Attributes.Take(al).ToArray();
                                }
                            }
                        }

                        // Create pattern list
                        var tpc = new TilePatternControl();
                        tpc.Initialize(Tile, TileList_Command, TileMain.Width, TileMain.Height);
                        TilePatternsList.Add(tpc);
                        wpTileList.Children.Add(tpc);
                        wpTileList.InvalidateMeasure();
                    }
                }
            }

            if (TilePatternsList.Count == 0)
            {
                TileList_AddTile(null);
                ctrlEditor.Initialize(Editor_Command);
                ctrlPreview.Initialize(null);
                ctrlProperties.Initialize(TileMain, TileProperties_Command);
            }
            else
            {
                ctrlEditor.Initialize(Editor_Command);
                ctrlPreview.Initialize(TilePatternsList[0].TileData);
                ctrlProperties.Initialize(TileMain, TileProperties_Command);
            }

            sldZoom.PropertyChanged += SldZoom_PropertyChanged;
            txtFrame.PropertyChanged += TxtFrame_PropertyChanged;

            btnClear.Tapped += BtnClear_Tapped;
            btnCut.Tapped += BtnCut_Tapped;
            btnCopy.Tapped += BtnCopy_Tapped;
            btnPaste.Tapped += BtnPaste_Tapped;
            btnHMirror.Tapped += BtnHMirror_Tapped;
            btnVMirror.Tapped += BtnVMirror_Tapped;
            btnRotateLeft.Tapped += BtnRotateLeft_Tapped;
            btnRotateRight.Tapped += BtnRotateRight_Tapped;
            btnShiftUp.Tapped += BtnShiftUp_Tapped;
            btnShiftRight.Tapped += BtnShiftRight_Tapped;
            btnShiftDown.Tapped += BtnShiftDown_Tapped;
            btnShiftLeft.Tapped += BtnShiftLeft_Tapped;
            btnMoveUp.Tapped += BtnMoveUp_Tapped;
            btnMoveRight.Tapped += BtnMoveRight_Tapped;
            btnMoveDown.Tapped += BtnMoveDown_Tapped;
            btnMoveLeft.Tapped += BtnMoveLeft_Tapped;
            btnInvert.Tapped += BtnInvert_Tapped;
            btnMask.Tapped += BtnMask_Tapped;
            btnExport.Tapped += BtnExport_Tapped;
            btnImport.Tapped += BtnImport_Tapped;

            btnUndo.Tapped += BtnUndo_Tapped;
            btnRedo.Tapped += BtnRedo_Tapped;

            btnViewAttributes.Tapped += BtnViewAttributes_Tapped;
            btnColorPicker.Tapped += BtnColorPicker_Tapped;
            btnInvertColorsCell.Tapped += BtnInvertColorsCell_Tapped;
            btnInvertPixelsCell.Tapped += BtnInvertPixelsCell_Tapped;

            btnTileInfoHide.Tapped += BtnTileInfoHide_Tapped;
            btnTileInfoShow.Tapped += BtnTileInfoShow_Tapped;

            Refresh();

            if (TilePatternsList.Count > 1)
            {
                TileList_Command(TilePatternsList.ElementAt(0), "SELECTED");
            }

            btnPaper.Tapped += BtnPaper_Click;
            btnInk.Tapped += BtnInk_Tapped;
            UpdateColorPanel();

            InitializeShortcuts();

            this.AddHandler(KeyDownEvent, Keyboard_Down, handledEventsToo: true);
            this.Focus();
            grdProcesando.IsVisible = false;
            _Modified = false;

            // Calcular altura inicial del WrapPanel
            CalculateWrapPanelHeight();

            // Suscribirse a cambios de tamaño para recalcular la altura
            wpTileList.SizeChanged += (s, e) => CalculateWrapPanelHeight();

            tileWidth = TileMain.Width;
            tileHeight = TileMain.Height;
        }


        #region Color

        private void BtnPaper_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ctrlColorPicker.IsVisible)
            {
                ctrlColorPicker.IsVisible = false;
                return;
            }
            var Tile = ctrlEditor.TileData;
            if (Tile.GraphicMode == GraphicsModes.Monochrome)
            {
                return;
            }
            ctrlColorPicker.IsVisible = true;
            ctrlColorPicker.Inicialize(Tile.GraphicMode, Tile.Palette, ctrlEditor.SecondaryColorIndex, ColorPickerPaper_Action);
        }


        private void BtnInk_Tapped(object? sender, TappedEventArgs e)
        {
            if (ctrlColorPicker.IsVisible)
            {
                ctrlColorPicker.IsVisible = false;
                return;
            }
            var Tile = ctrlEditor.TileData;
            if (Tile.GraphicMode == GraphicsModes.Monochrome)
            {
                return;
            }
            ctrlColorPicker.IsVisible = true;
            ctrlColorPicker.Inicialize(Tile.GraphicMode, Tile.Palette, ctrlEditor.PrimaryColorIndex, ColorPickerInk_Action);
        }


        private void UpdateColorPanel()
        {
            var Tile = ctrlEditor.TileData;
            if (Tile == null)
            {
                return;
            }
            switch (Tile.GraphicMode)
            {
                case GraphicsModes.Monochrome:
                    {
                        var ink = Tile.Palette[1];
                        var paper = Tile.Palette[0];
                        grdPaper.Background = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
                        txtPaper.Foreground = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
                        txtPaper.Text = "0";
                        grdInk.Background = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
                        txtInk.Foreground = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
                        txtInk.Text = "1";
                    }
                    break;

                case GraphicsModes.ZXSpectrum:
                case GraphicsModes.Next:
                    {
                        var ink = Tile.Palette[ctrlEditor.PrimaryColorIndex];
                        var paper = Tile.Palette[ctrlEditor.SecondaryColorIndex];
                        grdPaper.Background = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
                        txtPaper.Foreground = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
                        txtPaper.Text = ctrlEditor.SecondaryColorIndex.ToString();
                        grdInk.Background = new SolidColorBrush(Color.FromRgb(ink.Red, ink.Green, ink.Blue));
                        txtInk.Foreground = new SolidColorBrush(Color.FromRgb(paper.Red, paper.Green, paper.Blue));
                        txtInk.Text = ctrlEditor.PrimaryColorIndex.ToString();
                    }
                    break;
            }
        }



        private void ColorPickerPaper_Action(string command, int indexColor)
        {
            ctrlEditor.SecondaryColorIndex = indexColor;
            UpdateColorPanel();
        }


        private void ColorPickerInk_Action(string command, int indexColor)
        {
            ctrlEditor.PrimaryColorIndex = indexColor;
            UpdateColorPanel();
        }

        #endregion


        #region TileList

        private void TileList_Command(TilePatternControl sender, string command)
        {
            switch (command)
            {
                case "ADD":
                    TileList_AddTile(null);
                    ctrlEditor.TileData = sender.TileData;
                    ctrlPreview.TileData = sender.TileData;
                    ctrlPreview.Refresh();
                    TileList_Modified(sender.TileData);
                    break;
                case "SELECTED":
                    TileList_Unselect(sender);
                    ctrlEditor.TileData = sender.TileData;
                    ctrlPreview.TileData = sender.TileData;
                    ctrlPreview.Refresh();
                    TileProperties_FrameUpdate(ctrlProperties, command);
                    txtFrame.Text = "0";
                    break;
            }
        }


        private void TilePreview_Command(TilePreviewControl sender, string command)
        {

        }


        private void TileProperties_Command(TilePropertiesControl sender, string command)
        {
            try
            {
                var selectedTile = TilePatternsList.FirstOrDefault(d => d.IsSelected)?.TileData;

                switch (command)
                {
                    case "MODE":
                        {
                            switch (sender.TileData.GraphicMode)
                            {
                                case GraphicsModes.Monochrome:
                                    ctrlEditor.PrimaryColorIndex = 1;
                                    ctrlEditor.SecondaryColorIndex = 0;
                                    break;
                                case GraphicsModes.ZXSpectrum:
                                    ctrlEditor.PrimaryColorIndex = 0;
                                    ctrlEditor.SecondaryColorIndex = 7;
                                    break;
                            }
                            ctrlEditor.TileData = selectedTile;
                            TileList_Modified(selectedTile);
                            UpdateColorPanel();
                        }
                        break;
                    case "NAME":
                        break;
                    case "SIZE":
                        ResizePatterns();
                        break;
                }
            }
            catch { }
        }


        private void ResizePatterns()
        {
            for (int n = 0; n < TilePatternsList.Count; n++)
            {
                var pattern = TilePatternsList[n];
                if (pattern != null && pattern.TileData != null)
                {
                    if (pattern.TileData.Patterns != null && pattern.TileData.Patterns.Count > 0)
                    {
                        pattern.TileData.Patterns[0] = ResizePattern(pattern.TileData.Patterns[0]);
                    }
                    pattern.TileData.Width = TileMain.Width;
                    pattern.TileData.Height = TileMain.Height;
                    pattern.Refresh();
                }
            }
            tileWidth = TileMain.Width;
            tileHeight = TileMain.Height;

            ctrlPreview.TileData.Width = TileMain.Width;
            ctrlPreview.TileData.Height = TileMain.Height;
            ctrlPreview.Refresh();
            ctrlEditor.TileData.Width = TileMain.Width;
            ctrlEditor.TileData.Height = TileMain.Height;
            ctrlEditor.Refresh();
        }


        private Pattern ResizePattern(Pattern pattern)
        {
            var pat = new Pattern()
            {
                Id = pattern.Id,
                Name = pattern.Name,
                Number = pattern.Number,
                RawData = new int[TileMain.Width * TileMain.Height],
                Attributes = new AttributeColor[(TileMain.Width / 8) * (TileMain.Height / 8)]
            };
            for (int n = 0; n < pat.Attributes.Length; n++)
            {
                pat.Attributes[n] = new AttributeColor()
                {
                    Paper = ctrlEditor.SecondaryColorIndex,
                    Ink = ctrlEditor.PrimaryColorIndex
                };
            }

            for (int y = 0; y < TileMain.Height; y++)
            {
                for (int x = 0; x < TileMain.Width; x++)
                {
                    int colorIndex = 0;
                    if (x < tileWidth && y < tileHeight)
                    {
                        colorIndex = pattern.RawData[(y * tileWidth) + x];
                    }

                    pat.RawData[(y * TileMain.Width) + x] = colorIndex;
                }
            }
            return pat;
        }


        private void TileProperties_FrameUpdate(TilePropertiesControl sender, string command)
        {
            if (sender.TileData == null)
            {
                return;
            }

            // TODO: Multiple frames per tile?
            /*
            int f = sender.TileData.Frames - 1;
            if (f < 0)
            {
                f = 0;
            }
            else if (f > 255)
            {
                f = 255;
            }
            txtFrame.Maximum = f;
            txtFrame.Text = f.ToString();
            txtFrame.UpdateLayout();
            */
            TileProperties_Command(sender, "REFRESH");
        }


        private void TileList_AddTile(ZXMapsTile TileData)
        {
            TileMain.Tiles.Add(TileData);

            TilePatternControl selectedTile = null;
            int id = 0;
            if (TilePatternsList.Count > 0)
            {
                var sds = TilePatternsList.Where(d => d.TileData != null);
                if (sds.Any())
                {
                    id = sds.Max(d => d.TileData.Id) + 1;
                }
            }
            for (int n = 0; n < TilePatternsList.Count; n++)
            {
                var spc = TilePatternsList[n];
                if (spc.TileData != null && spc.TileData.Id < 0)
                {
                    spc.TileData.Id = id;
                    if (string.IsNullOrEmpty(spc.TileData.Name))
                    {
                        spc.TileData.Name = "Tile " + spc.TileData.Id.ToString();
                    }
                    spc.TileData.Export = true;
                    selectedTile = spc;
                    break;
                }
                if (spc.TileData == null && TileData != null)
                {
                    TilePatternsList[n].TileData = TileData;
                    selectedTile = TilePatternsList[n];
                    break;
                }
            }

            // Add void Tile
            var TilePattern = new TilePatternControl();
            TilePatternsList.Add(TilePattern);
            wpTileList.Children.Add(TilePattern);
            TilePattern.Initialize(null, TileList_Command, TileMain.Width, TileMain.Height);
            wpTileList.InvalidateMeasure();
            CalculateWrapPanelHeight();

            TileList_Unselect(selectedTile);
        }


        private void TileList_Unselect(TilePatternControl selected)
        {
            foreach (var control in TilePatternsList)
            {
                if (control == selected)
                {
                    control.IsSelected = true;
                }
                else
                {
                    control.IsSelected = false;
                }
            }
        }


        private void TileList_Delete(TilePatternControl control)
        {
            TilePatternsList.Remove(control);
            wpTileList.Children.Remove(control);
            ctrlEditor.TileData = null;
            ctrlPreview.TileData = null;
            ctrlProperties.TileData = null;
            control = null;
            CalculateWrapPanelHeight();
        }


        private void TileList_Insert(ZXMapsTile TileData)
        {
            var current = TileData.CurrentFrame;
            var curPat = TileData.Patterns[current];

            var pat = curPat.Clonar<Pattern>();
            pat.RawData = new int[pat.RawData.Length];

            var pats = TileData.Patterns.Take(current).ToList();
            pats.Add(pat);
            pats.AddRange(TileData.Patterns.Skip(current));
            TileData.Patterns = pats;
            TileData.Frames++;
            ctrlProperties.Refresh();
            ctrlEditor.Refresh();
            txtFrame.MaxHeight = TileData.Frames - 1;
        }


        private void TileList_Clone(ZXMapsTile TileData)
        {
            TilePatternControl selectedTile = null;
            int id = TilePatternsList.Where(d => d.TileData != null).Max(d => d.TileData.Id) + 1;
            var spc = TilePatternsList.FirstOrDefault(d => d.TileData == null);
            if (spc == null)
            {
                return;
            }
            var sd = TileData.Clonar<ZXMapsTile>();
            sd.Id = id;
            sd.Name = TileData.Name + " - copy";
            spc.TileData = sd;

            var TilePattern = new TilePatternControl();
            TilePatternsList.Add(TilePattern);
            wpTileList.Children.Add(TilePattern);
            TilePattern.Initialize(null, TileList_Command, TileMain.Width, TileMain.Height);
            wpTileList.InvalidateMeasure();
            CalculateWrapPanelHeight();

            spc.Select();
        }


        private void TileList_Modified(ZXMapsTile TileData)
        {
            if (!_Modified)
            {
                _Modified = true;
                DocumentModified?.Invoke(this, EventArgs.Empty);
            }

            if (TileData == null)
            {
                return;
            }
            var ctrl = TilePatternsList.FirstOrDefault(d => d.TileData != null && d.TileData.Id == TileData.Id);
            if (ctrl != null)
            {
                ctrl.TileData = TileData;
                ctrl.Refresh();
            }

            wpTileList.ItemWidth = (TileMain.Width * 4) + 4;
            wpTileList.ItemHeight = (TileMain.Height * 4) + 4;

            // Calcular altura del WrapPanel basada en el contenido
            CalculateWrapPanelHeight();
            wpTileList.InvalidateMeasure();
        }

        private void CalculateWrapPanelHeight()
        {
            if (wpTileList.ItemWidth <= 0 || wpTileList.ItemHeight <= 0 || wpTileList.Children.Count == 0)
            {
                return;
            }

            // Obtener el ancho disponible del ScrollViewer padre
            double availableWidth = wpTileList.Bounds.Width;

            // Si el ancho no está disponible aún, intentar obtenerlo del control padre
            if (availableWidth <= 0)
            {
                var parent = wpTileList.Parent as Control;
                if (parent != null)
                {
                    availableWidth = parent.Bounds.Width;
                }
            }

            // Si aún no está disponible, usar una estimación basada en el número de elementos
            if (availableWidth <= 0)
            {
                // Estimación: asumir que el ScrollViewer tiene aproximadamente 160-180 px de ancho
                // Esto es relativo al contenedor padre
                availableWidth = 160;
            }

            // Asegurar que hay espacio suficiente
            if (availableWidth < wpTileList.ItemWidth)
            {
                availableWidth = wpTileList.ItemWidth;
            }

            // Calcular el número de columnas que caben
            int itemsPerRow = Math.Max(1, (int)(availableWidth / wpTileList.ItemWidth));

            // Calcular el número de filas necesarias
            int totalRows = Math.Max(1, (int)Math.Ceiling((double)wpTileList.Children.Count / itemsPerRow));

            // Calcular la altura total (número de filas * altura de cada item)
            double calculatedHeight = totalRows * wpTileList.ItemHeight;

            wpTileList.Height = calculatedHeight;
        }


        private void BtnTileInfoHide_Tapped(object? sender, TappedEventArgs e)
        {
            btnTileInfoShow.IsVisible = true;
            btnTileInfoHide.IsVisible = false;
            foreach(var ctrl in TilePatternsList)
            {
                ctrl.InfoVisible = false;
            }
        }

        private void BtnTileInfoShow_Tapped(object? sender, TappedEventArgs e)
        {
            btnTileInfoShow.IsVisible = false;
            btnTileInfoHide.IsVisible = true;
            foreach (var ctrl in TilePatternsList)
            {
                ctrl.InfoVisible = true;
            }
        }



        #endregion


        #region Tile editor

        private void Editor_Command(TilePatternEditor sender, string command)
        {
            try
            {
                switch (command)
                {
                    case "REFRESH":
                        {
                            var cur = TilePatternsList.FirstOrDefault(d => d.IsSelected);
                            if (cur != null)
                            {
                                cur.TileData = sender.TileData;
                                cur.Refresh();
                                TileList_Modified(sender.TileData);
                                ctrlProperties.TileData = TileMain;
                                ctrlProperties.PrimaryColor = sender.PrimaryColorIndex;
                                ctrlProperties.SecondaryColor = sender.SecondaryColorIndex;
                                ctrlPreview.TileData = sender.TileData;

                                btnColorPicker.IsChecked = !ctrlEditor.ColorPicker;
                                UpdateColorPanel();
                            }
                        }
                        break;
                }
            }
            catch { }
        }

        #endregion


        #region Main editor


        private void Refresh()
        {
            // TODO: Multiple frames per tile?
            /*
            txtFrame.Text = actualFrame.ToString();
            if (ctrlProperties.TileData != null)
            {
                txtFrame.Maximum = ctrlProperties.TileData.Frames - 1;
            }
            else
            {
                txtFrame.Maximum = 0;
            }
            txtFrame.UpdateLayout();
            */
        }


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
            txtZoom.Text = "Zoom " + z.ToString() + "x";
            ctrlEditor.Zoom = z;
        }


        private void ZoomIn()
        {
            int v = sldZoom.Value.ToInteger();
            if (v < zooms.Length - 1)
            {
                sldZoom.Value = v - 1;
            }
        }


        private void ZoomOut()
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
            if (ctrlEditor.TileData != null)
            {
                ctrlEditor.TileData.CurrentFrame = actualFrame;
                ctrlEditor.Refresh();
            }
            Refresh();
            */
        }

        #endregion


        #region ToolBar

        /// <summary>
        /// Clear click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClear_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Clear();
        }

        /// <summary>
        /// Cut click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCut_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Cut();
        }


        //Copy click
        private void BtnCopy_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Copy();
        }


        /// <summary>
        /// Paste click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPaste_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Paste();
        }


        /// <summary>
        /// Horizontal mirror click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnHMirror_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.HorizontalMirror();
        }


        /// <summary>
        /// Vertical mirror click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnVMirror_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.VerticalMirror();
        }


        /// <summary>
        /// Rotate left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRotateLeft_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.RotateLeft();
        }


        /// <summary>
        /// Rotate right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRotateRight_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.RotateRight();
        }


        /// <summary>
        /// Shift up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnShiftUp_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.ShiftUp();
        }


        /// <summary>
        /// Shift right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnShiftRight_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.ShiftRight();
        }


        /// <summary>
        /// Shift down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnShiftDown_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.ShiftDown();
        }


        /// <summary>
        /// Shift left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnShiftLeft_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.ShiftLeft();
        }


        /// <summary>
        /// Move up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveUp_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.MoveUp();
        }


        /// <summary>
        /// Move right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveRight_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.MoveRight();
        }


        /// <summary>
        /// Move down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveDown_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.MoveDown();
        }


        /// <summary>
        /// Move left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveLeft_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.MoveLeft();
        }


        /// <summary>
        /// Invert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnInvert_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Invert();
        }


        /// <summary>
        /// Mask
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMask_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.ctrlEditor.Mask();
        }


        private void BtnExport_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Export();
        }


        private void Export()
        {
            /*
            var dlg = new TileExportDialog();
            dlg.Initialize(FileName, TilePatternsList.Select(d => d.TileData));
            dlg.ShowDialog(this.VisualRoot as Window);
            */
        }


        private void BtnImport_Tapped(object? sender, TappedEventArgs e)
        {
            Import();
        }



        private void BtnViewAttributes_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.ViewAttributes = btnViewAttributes.IsChecked == true;
        }

        private void BtnColorPicker_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.ColorPicker = btnColorPicker.IsChecked == false;
        }


        private void BtnInvertPixelsCell_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.InvertPixelsCell = btnInvertPixelsCell.IsChecked == false;
            if (ctrlEditor.InvertPixelsCell)
            {
                btnInvertColorsCell.IsChecked = true;
                ctrlEditor.InvertColorsCell = false;
            }
        }


        private void BtnInvertColorsCell_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.InvertColorsCell = btnInvertColorsCell.IsChecked == false;
            if (ctrlEditor.InvertColorsCell)
            {
                btnInvertPixelsCell.IsChecked = true;
                ctrlEditor.InvertPixelsCell = false;
            }
        }


        private void BtnRedo_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.Redo();
        }


        private void BtnUndo_Tapped(object? sender, TappedEventArgs e)
        {
            ctrlEditor.Undo();
        }

        #endregion


        #region Keyboard shortcus

        private Dictionary<Guid, Action> _keybCommands = new Dictionary<Guid, Action>();

        internal static Dictionary<string, ZXKeybCommand> keyboardCommands = new Dictionary<string, ZXKeybCommand> {
            { "Save", new ZXKeybCommand{ CommandId = Guid.Parse("87f7d73b-d28a-44f4-ba0c-41baa4de238c"), CommandName = "Save", Key = Key.S, Modifiers = KeyModifiers.Control } },
            { "Copy",new ZXKeybCommand{ CommandId = Guid.Parse("fee014bb-222b-42e3-80f3-048325b70e34"), CommandName = "Copy", Key = Key.C, Modifiers = KeyModifiers.Control } },
            { "Cut",new ZXKeybCommand{ CommandId = Guid.Parse("1edf352f-238b-421e-b69f-613dc63c0e47"), CommandName = "Cut", Key = Key.X, Modifiers = KeyModifiers.Control } },
            { "Paste", new ZXKeybCommand{ CommandId = Guid.Parse("f5d450b0-d126-4f62-885b-b3e28e638542"), CommandName = "Paste", Key = Key.V, Modifiers = KeyModifiers.Control } },
            { "Undo", new ZXKeybCommand{ CommandId = Guid.Parse("912c1887-ab37-4c0a-9aee-65d84b4521c7"), CommandName = "Undo", Key = Key.Z, Modifiers = KeyModifiers.Control } },
            { "Redo", new ZXKeybCommand{ CommandId = Guid.Parse("c5c506f0-d5e4-429f-ad21-af8cee7d1d9a"), CommandName = "Redo", Key = Key.Z, Modifiers = KeyModifiers.Control | KeyModifiers.Shift } },

            { "Clear", new ZXKeybCommand{ CommandId = Guid.Parse("246e1449-0a64-4327-a8d1-f1dbecbc69d2"), CommandName = "Clear", Key = Key.Q, Modifiers = KeyModifiers.Control | KeyModifiers.Shift } },
            { "Rotate Right", new ZXKeybCommand{ CommandId = new Guid("490e8830-f268-48f3-8989-92d6e22b8790"), CommandName = "Rotate Right", Key = Key.R, Modifiers = KeyModifiers.None } },
            { "Rotate Left", new ZXKeybCommand{ CommandId = new Guid("1da1bae1-2242-4061-9fa2-9265fc7f74b1"), CommandName = "Rotate Left", Key = Key.R, Modifiers = KeyModifiers.Shift } },
            { "Horizontal Mirror", new ZXKeybCommand{ CommandId = new Guid("aad6fb75-15c7-4f09-aa1c-af1e1b62f849"), CommandName = "Horizontal Mirror", Key = Key.H, Modifiers = KeyModifiers.None } },
            { "Vertical Mirror", new ZXKeybCommand{ CommandId = new Guid("3e20f906-c2e5-46b1-aa6d-8ee503769917"), CommandName = "Vertical Mirror", Key = Key.V, Modifiers = KeyModifiers.None } },
            { "Shift Up", new ZXKeybCommand{ CommandId = new Guid("7a2f0381-b603-4c5f-aefd-08ebafc74e5c"), CommandName = "Shift Up", Key = Key.Up, Modifiers = KeyModifiers.None } },
            { "Shift Right", new ZXKeybCommand{ CommandId = new Guid("75a310dd-66ec-41a5-bc8d-1b3408fd4f41"), CommandName = "Shift Right", Key = Key.Right, Modifiers = KeyModifiers.None } },
            { "Shift Down", new ZXKeybCommand{ CommandId = new Guid("2a73e9cd-f318-4a15-bdc7-daf35429c6e5"), CommandName = "Shift Down", Key = Key.Down, Modifiers = KeyModifiers.None } },
            { "Shift Left", new ZXKeybCommand{ CommandId = new Guid("1c8f1ec5-596c-4e7f-9fb6-9d5ff527b37a"), CommandName = "Shift Left", Key = Key.Left, Modifiers = KeyModifiers.None } },
            { "Move Up", new ZXKeybCommand{ CommandId = new Guid("6c202c3d-921f-43f7-b810-33febc8ad947"), CommandName = "Move Up", Key = Key.Up, Modifiers = KeyModifiers.Shift } },
            { "Move Right", new ZXKeybCommand{ CommandId = new Guid("8e2e96d0-0aa4-426e-a340-32cb53368000"), CommandName = "Move Right", Key = Key.Right, Modifiers = KeyModifiers.Shift } },
            { "Move Down", new ZXKeybCommand{ CommandId = new Guid("bdd1c206-e3ec-4a7d-8e61-42b620097b31"), CommandName = "Move Down", Key = Key.Down, Modifiers = KeyModifiers.Shift } },
            { "Move Left", new ZXKeybCommand{ CommandId = new Guid("6a6b98cf-5427-4d85-a230-6ab18544e312"), CommandName = "Move Left", Key = Key.Left, Modifiers = KeyModifiers.Shift } },
            { "Invert", new ZXKeybCommand{ CommandId = new Guid("e88125e8-f671-4085-ae8c-8d1f4866738a"), CommandName = "Invert", Key = Key.I, Modifiers = KeyModifiers.None } },
            { "Mask", new ZXKeybCommand{ CommandId = new Guid("efd5a1f4-cab8-4003-8370-8adf81176081"), CommandName = "Mask", Key = Key.M, Modifiers = KeyModifiers.None } },
            { "Export", new ZXKeybCommand{ CommandId = new Guid("321119c0-5c9b-40b5-9385-a62ca013f83c"), CommandName = "Export", Key = Key.E, Modifiers = KeyModifiers.None } },
            { "Zoom In", new ZXKeybCommand{ CommandId = new Guid("7e958c3d-c135-46b2-b041-63b8c7828787"), CommandName = "Zoom In", Key = Key.Add, Modifiers = KeyModifiers.None } },
            { "Zoom Out", new ZXKeybCommand{ CommandId = new Guid("d69843bb-8ad0-4eea-be58-29646dddaeed"), CommandName = "Zoom Out", Key = Key.Subtract, Modifiers = KeyModifiers.None } },
        };


        private void InitializeShortcuts()
        {
            DisableCommand(ApplicationCommands.Cut);
            DisableCommand(ApplicationCommands.Copy);
            DisableCommand(ApplicationCommands.Paste);

            _keybCommands = new Dictionary<Guid, Action>()
            {
                { keyboardCommands["Save"].CommandId, () => { RequestSaveDocument?.Invoke(this, EventArgs.Empty); } },
                { keyboardCommands["Copy"].CommandId, () => { ctrlEditor.Copy(); } },
                { keyboardCommands["Cut"].CommandId, () => { ctrlEditor.Cut(); } },
                { keyboardCommands["Paste"].CommandId, () => { ctrlEditor.Paste(); } },
                { keyboardCommands["Undo"].CommandId, () => { ctrlEditor.Undo(); } },
                { keyboardCommands["Redo"].CommandId, () => { ctrlEditor.Redo(); } },

                { keyboardCommands["Clear"].CommandId, () => { ctrlEditor.Clear(); } },
                { keyboardCommands["Rotate Right"].CommandId, () => { ctrlEditor.RotateRight(); } },
                { keyboardCommands["Rotate Left"].CommandId, () => { ctrlEditor.RotateLeft(); } },
                { keyboardCommands["Horizontal Mirror"].CommandId, () => { ctrlEditor.HorizontalMirror(); } },
                { keyboardCommands["Vertical Mirror"].CommandId, () => { ctrlEditor.VerticalMirror(); } },
                { keyboardCommands["Shift Up"].CommandId, () => { ctrlEditor.ShiftUp(); } },
                { keyboardCommands["Shift Right"].CommandId, () => { ctrlEditor.ShiftRight(); } },
                { keyboardCommands["Shift Down"].CommandId, () => { ctrlEditor.ShiftDown(); } },
                { keyboardCommands["Shift Left"].CommandId, () => { ctrlEditor.ShiftLeft(); } },
                { keyboardCommands["Move Up"].CommandId, () => { ctrlEditor.MoveUp(); } },
                { keyboardCommands["Move Right"].CommandId, () => { ctrlEditor.MoveRight(); } },
                { keyboardCommands["Move Down"].CommandId, () => { ctrlEditor.MoveDown(); } },
                { keyboardCommands["Move Left"].CommandId, () => { ctrlEditor.MoveLeft(); } },
                { keyboardCommands["Invert"].CommandId, () => { ctrlEditor.Invert(); } },
                { keyboardCommands["Mask"].CommandId, () => { ctrlEditor.Mask(); } },
                { keyboardCommands["Export"].CommandId, () => { Export(); } },
                { keyboardCommands["Zoom In"].CommandId, () => { ZoomIn(); } },
                { keyboardCommands["Zoom Out"].CommandId, () => { ZoomOut(); } },
            };
        }


        private void DisableCommand(RoutedCommand cut)
        {
            var field = typeof(RoutedCommand).GetField("<Gesture>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            KeyGesture g = new KeyGesture(Key.None, KeyModifiers.None);
            field.SetValue(cut, g);
        }

        private void Keyboard_Down(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            var commandId = ZXKeybMapper.GetCommandId(documentTypeId, e.Key, e.KeyModifiers);

            if (commandId != null && _keybCommands.ContainsKey(commandId.Value))
            {
                _keybCommands[commandId.Value]();
            }
        }

        #endregion


        #region Import

        private void Import()
        {
            /*
            var dlg = new TileImportDialog();
            dlg.Initialize(FileName, TilePatternsList.Select(d => d.TileData), Import_Command);
            dlg.ShowDialog(this.VisualRoot as Window);
            */
        }


        private void Import_Command(ZXMapsTile Tile, string command)
        {
            try
            {
                switch (command)
                {
                    case "ADD":
                        TileList_AddTile(Tile);
                        ctrlEditor.TileData = Tile;
                        ctrlPreview.TileData = Tile;
                        ctrlPreview.Refresh();
                        ctrlProperties.TileData = TileMain;
                        //TileList_Modified(sender.TileData);
                        break;
                    case "UPDATE":
                        //var spr = TilePatternsList.FirstOrDefault(d => d.Name == Tile.Name);
                        //if (spr != null)
                        //{
                        //    TileList_Modified(Tile);
                        //}
                        break;
                }
            }
            catch { }
        }


        #endregion
    }
}
