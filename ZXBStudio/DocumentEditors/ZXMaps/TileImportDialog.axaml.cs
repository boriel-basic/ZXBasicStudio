using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ZXBasicStudio.Classes;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentModel.Classes;
using ZXBasicStudio.DocumentModel.Interfaces;
using ZXBasicStudio.Extensions;
using ZXBasicStudio.IntegratedDocumentTypes.CodeDocuments.Basic;
using static System.Net.Mime.MediaTypeNames;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using System.Formats.Tar;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class TileImportDialog : Window, IDisposable
    {
        /*
        private string fileName = "";


        private FileTypeConfig fileType = null;
        private Pattern[] patterns = null;
        private ExportConfig exportConfig = null;
        */

        private Action<ZXMapsTile, string> CallBackCommand = null;
        private IEnumerable<ZXMapsTile> tiles = null;
        private ZXMapsTile tile = null;

        private GraphicsModes tileMode = GraphicsModes.Monochrome;
        private int tileWidth = 16;
        private int tileHeight = 16;
        private int tileZoom = 4;
        private int cutOff = 5;
        private int tileFrames = 1;
        private bool appendToTiles = false;
        private bool insertBlankAtStart = true;
        private bool removeDuplicatedTiles = true;
        private DispatcherTimer tmr = null;
        private bool resetFocus = false;


        public TileImportDialog()
        {
            InitializeComponent();

            btnFile.Tapped += BtnFile_Tapped;
            sldZoom.PropertyChanged += SldZoom_PropertyChanged;

            grdImport.KeyDown += GrdImport_KeyDown;

            cmbMode.SelectionChanged += CmbMode_SelectionChanged;
            txtWidth.ValueChanged += TxtWidth_ValueChanged;
            txtHeight.ValueChanged += TxtHeight_ValueChanged;
            sldCutOff.PropertyChanged += sldCutOff_PropertyChanged;

            btnCancel.Tapped += BtnCancel_Tapped;
            btnImport.Tapped += BtnImport_Tapped;
        }

        public bool Initialize(IEnumerable<ZXMapsTile> tiles, Action<ZXMapsTile, string> callBackCommand)
        {
            this.CallBackCommand = callBackCommand;
            this.tiles = tiles;

            tileMode = GraphicsModes.Monochrome;
            tileWidth = 16;
            tileHeight = 16;

            cmbMode.SelectedIndex = 0;
            txtWidth.Text = tileWidth.ToString();
            txtHeight.Text = tileHeight.ToString();
            sldZoom.Value = tileZoom;

            tmr = new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Normal, UpdatePreview);

            return true;
        }

        public void Dispose()
        {
            if (tmr != null)
            {
                tmr.Stop();
                tmr = null;
            }
        }

        #region Image source

        private async void BtnFile_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[5];
            fileTypes[0] = new FilePickerFileType("All files") { Patterns = new[] { "*", "*.*" } };
            fileTypes[1] = new FilePickerFileType("BMP files") { Patterns = new[] { "*.bmp" } };
            fileTypes[2] = new FilePickerFileType("JPG files") { Patterns = new[] { "*.jpg", "*.jpeg" } };
            fileTypes[3] = new FilePickerFileType("PNG files") { Patterns = new[] { "*.png" } };
            fileTypes[4] = new FilePickerFileType("ZX Paintbrush SCR files") { Patterns = new[] { "*.scr" } };
            /*fileTypes[5] = new FilePickerFileType("SCR Spectrum screen files") { Patterns = new[] { "*.scr" } };*/

            var select = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = fileTypes,
                Title = "Select file to import...",
            });

            if (select != null && select.Count > 0)
            {
                cnvSource.LoadImage(select[0]);
            }
        }


        private int lastZoom = 0;

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

            txtZoom.Text = "Zoom " + z.ToString() + "x";
            cnvSource.Zoom = z;
        }


        /// <summary>
        /// CutOff changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sldCutOff_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            cutOff = (int)sldCutOff.Value;
        }


        private void GrdImport_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Avalonia.Input.Key.Up:
                    if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Shift)
                    {
                        cnvSource.OffsetY += 8;
                    }
                    else
                    {
                        cnvSource.OffsetY++;
                    }
                    break;
                case Avalonia.Input.Key.Down:
                    if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Shift)
                    {
                        cnvSource.OffsetY -= 8;
                    }
                    else
                    {
                        cnvSource.OffsetY--;
                    }
                    break;
                case Avalonia.Input.Key.Left:
                    if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Shift)
                    {
                        cnvSource.OffsetX += 8;
                    }
                    else
                    {
                        cnvSource.OffsetX++;
                    }
                    break;
                case Avalonia.Input.Key.Right:
                    if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Shift)
                    {
                        cnvSource.OffsetX -= 8;
                    }
                    else
                    {
                        cnvSource.OffsetX--;
                    }
                    break;
            }
        }

        #endregion


        #region Preview


        private void UpdatePreview(object userState, EventArgs e)
        {
            _UpdatePreview();
            if (resetFocus)
            {
                btnFile.Focus();
                resetFocus = false;
            }
        }


        private void _UpdatePreview()
        {
            try
            {
                ReadProperties();

                var imgData = cnvSource.imageData;
                if (imgData == null)
                {
                    return;
                }

                GetProperties();

                var s = new ZXMapsTile();
                s.CurrentFrame = 0;
                s.DefaultColor = 7;
                s.Export = true;
                s.Frames = 1;
                s.GraphicMode = tileMode;
                s.Height = tileHeight;
                s.Id = 0;
                s.Masked = false;
                s.Name = "";
                s.Palette = ServiceLayer.GetPalette(tileMode);
                s.Patterns = new List<Pattern>();
                s.Width = tileWidth;

                int numAttr = (tileWidth / 8) * (tileHeight / 8);
                var attrsDefault = new AttributeColor[numAttr];

                for (int n = 0; n < numAttr; n++)
                {
                    var attr = new AttributeColor();
                    attr.Paper = 7;
                    attr.Ink = 0;
                    attr.Bright = false;
                    attr.Flash = false;
                    attrsDefault[n] = attr;
                }

                var w = scrPreview.Bounds.Width;
                var h = scrPreview.Bounds.Width;
                double ix = (tileWidth + 2) * 4;
                double iy = (tileHeight + 2) * 4;
                int sw = tileWidth / 8;
                int sh = tileHeight / 8;
                int px = 0;
                int py = 0;
                int xs = 0;
                int ys = 0;
                int paper = -1;
                int ink = -1;
                int prevX = 0;
                int prevY = 0;
                int anchoPreview = 0;
                int altoPreview = 0;
                int maxX = (int)(cnvSource.Width / tileWidth);
                int maxY = (int)(cnvSource.Height / tileHeight);

                pnlPreview.Children.Clear();

                tileFrames = maxX * maxY;

                int frameNumber = 0;
                int framesX = imgData.Width / tileWidth;
                int framesY = imgData.Height / tileHeight;

                for (int fy = 0; fy < framesY; fy++)
                {
                    cnvSource.OffsetY = fy * tileHeight;
                    for (int fx = 0; fx < framesX; fx++)
                    {
                        cnvSource.OffsetX = fx * tileWidth;

                        var attrs = attrsDefault.Clonar<AttributeColor[]>();
                        var pattern = new Pattern();
                        pattern.Attributes = attrs; // new AttributeColor[(tileWidth / 8) * (tileHeight / 8)];
                        pattern.Id = frameNumber;
                        pattern.Name = "";
                        pattern.Number = frameNumber.ToString();
                        pattern.RawData = new int[tileWidth * tileHeight];

                        int dir = 0;
                        for (int cy = 0; cy < sh; cy++)
                        {
                            py = cy * 8;
                            for (int cx = 0; cx < sw; cx++)
                            {
                                //ys = cnvSource.OffsetY + py;
                                ys = (fy * tileHeight) + py;

                                px = cx * 8;
                                paper = -1;
                                ink = -1;
                                for (int y = 0; y < 8; y++)
                                {
                                    //xs = (cnvSource.OffsetX /*+ (n * tileWidth)*/) + px;
                                    xs = (fx * tileWidth) + px;

                                    dir = (((cy * 8) + y) * tileWidth) + (cx * 8);
                                    for (int x = 0; x < 8; x++)
                                    {
                                        if (xs >= 0 && xs < imgData.Width &&
                                            ys >= 0 && ys < imgData.Height)
                                        {
                                            var c = imgData[xs, ys];
                                            var idxAttr = GetColor(c.R, c.G, c.B, s.Palette);
                                            if (tileMode == GraphicsModes.ZXSpectrum)
                                            {
                                                // Fijar el color
                                                var dirAttr = (cy * sw) + cx;
                                                var attr = pattern.Attributes[dirAttr];
                                                attr.Bright = attr.Bright | (idxAttr > 7);
                                                byte cCol = (byte)(idxAttr /*& 0b111*/);
                                                if (paper == -1)
                                                {
                                                    attr.Paper = cCol;
                                                    paper = cCol;
                                                }
                                                else if (ink == -1 && paper != cCol)
                                                {
                                                    attr.Ink = cCol;
                                                    ink = cCol;
                                                }
                                                if (cCol == paper)
                                                {
                                                    idxAttr = 0;
                                                }
                                                else
                                                {
                                                    idxAttr = 7;
                                                }
                                            }
                                            pattern.RawData[dir] = idxAttr;
                                        }
                                        else
                                        {
                                            pattern.RawData[dir] = 0;
                                        }
                                        dir++;
                                        xs++;
                                    }
                                    ys++;
                                }
                            }
                        }

                        bool addToPatterns = true;
                        if (removeDuplicatedTiles)
                        {
                            if (PatternExists(s.Patterns, pattern))
                            {
                                addToPatterns = false;
                            }
                        }
                        if (addToPatterns)
                        {
                            s.Patterns.Add(pattern);

                            var prev = new ZXGraphics.ZXSpriteImage();
                            var img = new Avalonia.Controls.Image();
                            img.Width = tileWidth * tileZoom;
                            img.Height = tileHeight * tileZoom;
                            img.Source = prev;
                            pnlPreview.Children.Add(img);

                            Canvas.SetLeft(img, prevX);
                            Canvas.SetTop(img, prevY);
                            prevX += (tileWidth * 4);

                            if (anchoPreview < prevX)
                            {
                                anchoPreview = prevX;
                            }
                            if ((prevX + sw) > w)
                            {
                                prevY += (tileHeight * 4);
                                prevX = 0;
                                if (altoPreview < prevY)
                                {
                                    altoPreview = prevY;
                                }
                            }

                            prev.RenderSprite(s, frameNumber);
                            frameNumber++;
                        }
                    }
                }

                cnvSource.OffsetX = 0;
                cnvSource.OffsetY = 0;

                tile = s;
                pnlPreview.Width = anchoPreview + (tileWidth * 4);
                pnlPreview.Height = altoPreview + (tileHeight * 4);
            }
            catch (Exception ex)
            {

            }
        }

        private bool PatternExists(List<Pattern> patterns, Pattern pattern)
        {
            foreach (var p1 in patterns)
            {
                bool igual = true;
                for (int n = 0; n < p1.RawData.Count(); n++)
                {
                    if (p1.RawData[n] != pattern.RawData[n])
                    {
                        igual = false;
                        break;
                    }
                }
                if (igual)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetColor(byte r, byte g, byte b, PaletteColor[] palette)
        {
            bool brigth = false;
            if (r > 250 || g > 250 || b > 250)
            {
                brigth = true;
            }
            byte cr = GetColor_CutOff(r);
            byte cg = GetColor_CutOff(g);
            byte cb = GetColor_CutOff(b);
            PaletteColor targetColor = new PaletteColor()
            {
                Blue = cb,
                Green = cg,
                Red = cr
            };

            var palColor = palette.OrderBy(c => GetColorDistance(c, targetColor)).First();
            for (int n = 0; n < palette.Count(); n++)
            {
                var p = palette[n];
                if (palColor.Red == p.Red &&
                    palColor.Green == p.Green &&
                    palColor.Blue == p.Blue)
                {
                    if (brigth && n < 8)
                    {
                        return n + 8;
                    }
                    return n;
                }
            }
            return 0;
        }


        private byte GetColor_CutOff(byte c)
        {
            if (cutOff == 5)
            {
                return c;
            }

            if (cutOff < 5)
            {
                int ic = c - (25 * (5 - cutOff));
                if (ic < 0)
                {
                    return 0;
                }
                else
                {
                    return ic.ToByte();
                }
            }
            else
            {
                int ic = c + (25 * (cutOff - 5));
                if (ic > 255)
                {
                    return 255;
                }
                else
                {
                    return ic.ToByte();
                }
            }
        }


        private static double GetColorDistance(PaletteColor c1, PaletteColor c2)
        {
            int rDiff = Math.Abs(c1.Red - c2.Red);
            int gDiff = Math.Abs(c1.Green - c2.Green);
            int bDiff = Math.Abs(c1.Blue - c2.Blue);
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }


        private void GetProperties()
        {
            tileWidth = txtWidth.Text.ToInteger();
            tileHeight = txtHeight.Text.ToInteger();
            tileMode = (GraphicsModes)cmbMode.SelectedIndex;
        }

        private void CmbMode_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            resetFocus = true;
            //btnFile.Focus();
        }


        private void ChangeMode(GraphicsModes oldMode, GraphicsModes newMode)
        {
            resetFocus = true;
        }


        private void TxtWidth_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            resetFocus = true;
            cnvSource.SpriteWidth = txtWidth.Text.ToInteger();
            cnvSource.Refresh();
        }

        private void TxtHeight_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            resetFocus = true;
            cnvSource.SpriteHeight = txtHeight.Text.ToInteger();
            cnvSource.Refresh();
        }

        #endregion


        #region Buttons

        private void BtnImport_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Import();
        }

        private void BtnCancel_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        #endregion


        #region Import

        private async void Import()
        {
            try
            {
                GetProperties();

                if (!appendToTiles)
                {
                    // Check if existing tiles an overwrite
                    if (tiles.Any())
                    {
                        if (!await this.ShowConfirm("Confirm overwrite", "The existing tiles will be deleted.\r\nAre you sure you want to overwrite it?"))
                        {
                            return;
                        }

                    }
                }

                ReadProperties();

                for (int n = 0; n < tile.Patterns.Count(); n++)
                {
                    var spr = tile.Clonar<ZXMapsTile>();
                    spr.Id = n;
                    spr.Patterns = spr.Patterns.Skip(n).Take(1).ToList();
                    spr.Frames = 1;
                    spr.Name = n.ToString();
                    var spr2 = tiles.FirstOrDefault(d => d != null && d.Name == spr.Name);
                    if (spr2 == null)
                    {
                        var id = 0;
                        if (tiles.Count() > 0 && tiles.ElementAt(0) != null)
                        {
                            id = tiles.Where(d => d != null).Max(d => d.Id) + 1;
                        }
                        spr.Id = id;
                        CallBackCommand?.Invoke(spr, "ADD");
                    }
                    else
                    {
                        spr.Id = spr2.Id;
                        CallBackCommand?.Invoke(tile, "UPDATE");
                    }
                }

                this.Close();
                this.Dispose();
            }
            catch (Exception ex)
            {
                this.ShowError("ERROR importing image", ex.Message + ex.StackTrace);
            }
        }


        private void ReadProperties()
        {
            tileWidth = txtWidth.Text.ToInteger();
            tileHeight = txtHeight.Text.ToInteger();
            appendToTiles = chkAppend.IsChecked.ToBoolean();
            insertBlankAtStart = chkInsertBlank.IsChecked.ToBoolean();
            removeDuplicatedTiles = chkRemoveDuplicated.IsChecked.ToBoolean();
        }

        #endregion
    }
}