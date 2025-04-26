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
using Avalonia.Controls.Shapes;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;
using FFmpeg.AutoGen;

namespace ZXBasicStudio.DocumentEditors.ZXGraphics
{
    public partial class PaletteBuilderDialog : Window, IDisposable
    {
        private bool StandartPalette = true;
        private bool ULAColors = false;
        private bool GrayScale = false;

        private Rectangle[] rectangulos = new Rectangle[256];
        private PaletteColor[] palette = null;
        private string sourceFile = null;
        private string convertedFile = null;
        private bool imgSourceLoaded = false;

        private int[,] pixels = null;
        private int selectedColorIndex = 0;
        private int transparentColor = 223;

        private Brush negro = new SolidColorBrush(Colors.Black);
        private Brush blanco = new SolidColorBrush(Colors.White);
        private Brush rojo = new SolidColorBrush(Colors.Red);


        public PaletteBuilderDialog()
        {
            InitializeComponent();

            if (!ServiceLayer.Initialized)
            {
                ServiceLayer.Initialize();
            }

            // Set the palette
            palette = ServiceLayer.GetPalette(GraphicsModes.Next);
            selectedColorIndex = 0;
            DrawPalette();

            btnFileSource.Tapped += BtnFileSource_Tapped;
            btnResetPalette.Click += BtnResetPalette_Click;
            btnLoadPalette.Click += BtnLoadPalette_Click;
            btnSavePalette.Click += BtnSavePalette_Click;
            ActivarEventosSlider();

            chkUseCurrent.Click += ChkUseCurrent_Click;
            chkAppend.Click += ChkAppend_Click;
            chkULA.Click += ChkULA_Click;
            chkGrayscale.Click += ChkGrayscale_Click;

            btnRefresh.Click += BtnRefresh_Click;
            btnSaveImage.Click += BtnSaveImage_Click;

            btnClose.Click += BtnClose_Click;
        }


        private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }


        private void ActivarEventosSlider()
        {
            sldRed.ValueChanged += Sld_ValueChanged;
            sldGreen.ValueChanged += Sld_ValueChanged;
            sldBlue.ValueChanged += Sld_ValueChanged;

            txtRed.ValueChanged += Txt_ValueChanged;
            txtGreen.ValueChanged += Txt_ValueChanged;
            txtBlue.ValueChanged += Txt_ValueChanged;
        }


        private void DesactivarEventosSlider()
        {
            sldRed.ValueChanged -= Sld_ValueChanged;
            sldGreen.ValueChanged -= Sld_ValueChanged;
            sldBlue.ValueChanged -= Sld_ValueChanged;

            txtRed.ValueChanged -= Txt_ValueChanged;
            txtGreen.ValueChanged -= Txt_ValueChanged;
            txtBlue.ValueChanged -= Txt_ValueChanged;
        }


        public void Dispose()
        {
        }


        public void DrawPalette()
        {
            int idx = 0;
            int ancho = 16;
            int alto = 16;

            cnvPalette.Children.Clear();
            rectangulos = new Rectangle[256];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var c = palette[idx];
                    Rectangle rect = new Rectangle();
                    rect.Width = ancho;
                    rect.Height = alto;
                    rect.Fill = new SolidColorBrush(Color.FromRgb(c.Red, c.Green, c.Blue));
                    rect.Margin = new Thickness(0, 0, 0, 0);
                    rect.StrokeThickness = 1;
                    if (selectedColorIndex == idx)
                    {
                        rect.Stroke = blanco;
                    }
                    else if (idx == 227)
                    {
                        rect.Stroke = rojo;
                    }
                    else
                    {
                        rect.Stroke = negro;
                    }
                    rect.Tag = idx;
                    rect.Tapped += Rect_Tapped;
                    ToolTip.SetTip(rect, $"Index: {idx}, RGB: {c.Red}, {c.Green}, {c.Blue}");
                    cnvPalette.Children.Add(rect);
                    Canvas.SetLeft(rect, x * ancho);
                    Canvas.SetTop(rect, y * alto);
                    rectangulos[idx] = rect;
                    idx++;
                }
            }

            UpdateColorTest();
            //UpdateSelectedColor();
        }

        private void UpdateSelectedColor()
        {
            var color = palette[selectedColorIndex];

            txtSelectedColor.Text = $"Selected color index: {selectedColorIndex}";
            txtRed.Text = color.Red.ToString();
            sldRed.Value = color.Red;
            txtGreen.Text = color.Green.ToString();
            sldGreen.Value = color.Green;

            //txtBlue.Text = color.Blue.ToString();
            //sldBlue.Value = color.Blue;
        }


        private void UpdateColorTest()
        {
            txtSelectedColor.Text = $"Selected color index: {selectedColorIndex}";
            var color = palette[selectedColorIndex];
            grdSelectedColor.Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
            txtRed.Text = color.Red.ToString();
            txtGreen.Text = color.Green.ToString();
            txtBlue.Text = color.Blue.ToString();
            sldRed.Value = color.Red;
            sldGreen.Value = color.Green;
            sldBlue.Value = color.Blue;

            rectangulos[selectedColorIndex].Fill = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
        }


        private void Sld_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            DesactivarEventosSlider();
            var color = palette[selectedColorIndex];

            color.Red = AjustarColor(sldRed.Value.ToInteger(), 36);
            color.Green = AjustarColor(sldGreen.Value.ToInteger(), 36);
            color.Blue = AjustarColor(sldBlue.Value.ToInteger(), 36);

            UpdateColorTest();
            ActivarEventosSlider();
        }


        private void Txt_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            DesactivarEventosSlider();
            var color = palette[selectedColorIndex];

            color.Red = AjustarColor(txtRed.Value.ToInteger(), 36);
            color.Green = AjustarColor(txtGreen.Value.ToInteger(), 36);
            color.Blue = AjustarColor(txtBlue.Value.ToInteger(), 36);

            UpdateColorTest();
            ActivarEventosSlider();
        }


        private void Rect_Tapped(object? sender, TappedEventArgs e)
        {
            DesactivarEventosSlider();
            var rect = (Rectangle)sender;
            var idx = rect.Tag;
            rectangulos[selectedColorIndex].Stroke = negro;
            selectedColorIndex = idx.ToInteger();
            rectangulos[selectedColorIndex].Stroke = blanco;
            UpdateColorTest();
            ActivarEventosSlider();
        }

        private void BtnFileSource_Tapped(object? sender, TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[4];
            fileTypes[0] = new FilePickerFileType("All files") { Patterns = new[] { "*", "*.*" } };
            fileTypes[1] = new FilePickerFileType("BMP files") { Patterns = new[] { "*.bmp" } };
            fileTypes[2] = new FilePickerFileType("JPG files") { Patterns = new[] { "*.jpg", "*.jpeg" } };
            fileTypes[3] = new FilePickerFileType("PNG files") { Patterns = new[] { "*.png" } };

            var select = StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = fileTypes,
                Title = "Select source file to use as source...",
            }).Result;

            if (select != null && select.Count > 0)
            {
                imgSourceLoaded = true;
                imgSource.LoadImage(select[0]);
                sourceFile = select[0].Path.ToString().Replace("file:///", "");
                grdSource.Width = imgSource.Width;
                grdSource.Height = imgSource.Height;
                Convertir();
            }
        }


        private void Convertir()
        {
            try
            {
                if (!imgSourceLoaded)
                {
                    return;
                }

                // Obtener la resolución y colores deseados
                int w = 256;
                int h = 192;
                int c = 256;

                switch (cmbMode.SelectedIndex)
                {
                    case 0:
                        w = 256;
                        h = 192;
                        c = 256;
                        break;

                    case 1:
                        w = 320;
                        h = 256;
                        c = 256;
                        break;
                }
                imgConverted.Width = w;
                imgConverted.Height = h;
                grdConverted.Width = w;
                grdConverted.Height = h;

                pixels = new int[w, h];

                if (chkUseCurrent.IsChecked == true)
                {
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            try
                            {
                                if (x < imgSource.Width && y < imgSource.Height)
                                {
                                    var p = imgSource.imageData[x, y];
                                    if (p.A == 0)
                                    {
                                        pixels[x, y] = 227;
                                    }
                                    else
                                    {
                                        var idx = ServiceLayer.GetColor(p.R, p.G, p.B, palette, 5);
                                        pixels[x, y] = idx;
                                    }
                                }
                                else
                                {
                                    pixels[x, y] = 0;
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    imgConverted.SetImageData(pixels, palette, w, h);
                }
                else
                {
                    int ini = 0;
                    // Si no añadimos a la paleta actual, la borramos
                    if (chkAppend.IsChecked != true)
                    {
                        palette = new PaletteColor[256];
                        for (int n = 0; n < 256; n++)
                        {
                            palette[n] = new PaletteColor()
                            {
                                Blue = 0,
                                Green = 0,
                                Red = 0
                            };
                        }
                    }
                    // Incluir colores ULA
                    if (chkULA.IsChecked == true)
                    {
                        var ulaP = ServiceLayer.GetPalette(GraphicsModes.ZXSpectrum);
                        for (int n = 0; n < 16; n++)
                        {
                            palette[n] = ulaP[n];
                        }
                        ini = 16;
                    }
                    // Incluir escala de grises
                    if (chkGrayscale.IsChecked == true)
                    {
                        var grayP = ServiceLayer.GetPalette(GraphicsModes.Monochrome);
                        int cl = 0;
                        for (int n = 0; n < 8; n++)
                        {
                            palette[ini++] = new PaletteColor()
                            {
                                Blue = (byte)cl,
                                Green = (byte)cl,
                                Red = (byte)cl
                            };
                            cl = cl + 36;
                            if (cl > 255)
                            {
                                cl = 255;
                            }
                        }
                    }

                    // Buscamos el primer color libre
                    for (int n = 255; n > 1; n--)
                    {
                        if (n != 227)
                        {
                            if (palette[n].Red != 0 ||
                                palette[n].Green != 0 ||
                                palette[n].Blue != 0)
                            {
                                ini = n + 1;
                                break;
                            }
                        }
                    }

                    // Recorremos la imagen original
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            try
                            {
                                if (x < imgSource.Width && y < imgSource.Height)
                                {
                                    // Redondeamos el color a multiplos de 36
                                    var p = imgSource.imageData[x, y];
                                    var pR = AjustarColor(p.R, 36);
                                    var pG = AjustarColor(p.G, 36);
                                    var pB = AjustarColor(p.B, 36);

                                    // Buscamos si ya existe el color
                                    bool encontrado = false;
                                    if (p.A == 0)
                                    {
                                        // Color transparente
                                        pixels[x, y] = 227;
                                        if (!encontrado)
                                        {
                                            palette[227] = new PaletteColor()
                                            {
                                                Blue = 255,
                                                Green = 0,
                                                Red = 255
                                            };
                                            encontrado = true;
                                        }
                                    }
                                    else
                                    {
                                        // Buscar el color
                                        for (int n = 0; n < 256; n++)
                                        {
                                            if (palette[n].Red == pR && palette[n].Green == pG && palette[n].Blue == pB)
                                            {
                                                pixels[x, y] = n;
                                                encontrado = true;
                                                break;
                                            }
                                        }
                                    }
                                    // Si no existe el color
                                    if (!encontrado)
                                    {
                                        // Si ya hay más de 256 colore, buscamos el más parecido
                                        if (ini > 255)
                                        {
                                            var idx = ServiceLayer.GetColor(p.R, p.G, p.B, palette, 5);
                                            pixels[x, y] = idx;
                                        }
                                        // Creamos el nuevo color
                                        else
                                        {
                                            palette[ini] = new PaletteColor()
                                            {
                                                Blue = pB,
                                                Green = pG,
                                                Red = pR
                                            };
                                            pixels[x, y] = ini;
                                            ini++;
                                        }
                                    }
                                }
                                else
                                {
                                    pixels[x, y] = 0;
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    imgConverted.SetImageData(pixels, palette, w, h);
                    DrawPalette();
                }
            }
            catch (Exception ex)
            {

            }
        }


        private byte AjustarColor(decimal color8, decimal unidad)
        {
            if (color8 > 255)
            {
                color8 = 255;
            }
            decimal cd = (color8 / unidad) + 0.5M;
            byte cb = (byte)cd;
            cb = (byte)(cb * (byte)unidad);
            return cb;
        }

        #region Save and Load palette

        private void BtnSavePalette_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[2] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };
            fileTypes[1] = new FilePickerFileType("Gimp palette (*.gpl)") { Patterns = new[] { "*.gpl" } };
            fileTypes[0] = new FilePickerFileType("Next palette (*.pal)") { Patterns = new[] { "*.pal" } };

            var select = StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select file to save palette...",
                ShowOverwritePrompt = true,
                DefaultExtension = "*.pal",
                FileTypeChoices = fileTypes
            }).Result;

            if (select != null)
            {
                var fileName = select.Path.LocalPath;
                var extension = System.IO.Path.GetExtension(fileName);

                bool error = true;
                switch (extension)
                {
                    case ".gpl":
                        error = !SavePalette_Gpl(fileName);
                        break;
                    case ".pal":
                        error = !SavePalette_Pal(fileName);
                        break;
                }
                if (error)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "SAVE ERROR",
                        ContentMessage = "An error occurred while saving the file.",
                        Icon = MsBox.Avalonia.Enums.Icon.Warning,
                        WindowIcon = ((Window)this.VisualRoot).Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                    box.ShowAsPopupAsync(this);
                }
            }
        }


        private bool SavePalette_Gpl(string fileName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("GIMP Palette");
            sb.AppendLine($"Name: {System.IO.Path.GetFileNameWithoutExtension(fileName)}");
            sb.AppendLine("#Generated with ZX Basic Studio");
            for (int i = 0; i < 256; i++)
            {
                var color = palette[i];
                sb.AppendLine($"{color.Red.ToString().PadLeft(3, ' ')} {color.Green.ToString().PadLeft(3, ' ')} {color.Blue.ToString().PadLeft(3, ' ')}\ti:{i},r:{color.Red},g:{color.Green},b:{color.Blue}");
            }
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            return ServiceLayer.Files_SaveFileData(fileName, data);
        }


        private bool SavePalette_Pal(string fileName)
        {
            var data = new List<byte>();
            foreach (var color in palette)
            {
                int r = color.Red / 36;
                int g = color.Green / 36;
                int b = color.Blue / 36;
                byte d1 = (byte)(((r & 0x07) << 5) | ((g & 0x07) << 2) | ((b & 0x7) >> 1));
                byte d2 = (byte)(b & 0x1);
                if (color.HasPriority)
                {
                    d2 = (byte)(d2 | 0x80);
                }
                data.Add(d1);
                data.Add(d2);
            }
            return ServiceLayer.Files_SaveFileData(fileName, data.ToArray());
        }


        private void BtnLoadPalette_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[2] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };
            fileTypes[1] = new FilePickerFileType("Gimp palette (*.gpl)") { Patterns = new[] { "*.gpl" } };
            fileTypes[0] = new FilePickerFileType("Next palette (*.pal)") { Patterns = new[] { "*.pal" } };

            var select = StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select file to load...",
                FileTypeFilter = fileTypes,
            }).Result;

            if (select != null && select.Count > 0)
            {
                var data = ServiceLayer.GetFileData(select[0].Path.LocalPath);
                if (data == null)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "LOAD ERROR",
                        ContentMessage = "An error occurred while loading the file.",
                        Icon = MsBox.Avalonia.Enums.Icon.Warning,
                        WindowIcon = ((Window)this.VisualRoot).Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                    box.ShowAsPopupAsync(this);
                }

                DesactivarEventosSlider();
                int idx = 0;
                for (int dir = 0; dir < 512; dir += 2)
                {
                    int d1 = data[dir];
                    int d2 = data[dir + 1];

                    int r = (d1 >> 5) & 0x07;
                    int g = (d1 >> 2) & 0x07;
                    int b = (d1 & 0x03) << 1;
                    b = b | (d2 & 0x01);
                    var color = palette[idx];
                    color.Red = (byte)(r * 36);
                    color.Green = (byte)(g * 36);
                    color.Blue = (byte)(b * 36);
                    color.HasPriority = d2 > 1;
                    idx++;
                }
                DrawPalette();
                UpdateColorTest();
                ActivarEventosSlider();
                Convertir();
            }
        }


        private void BtnResetPalette_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DesactivarEventosSlider();
            palette = ServiceLayer.GetPalette(GraphicsModes.Next);
            UpdateColorTest();
            ActivarEventosSlider();
            DrawPalette();
            chkUseCurrent.IsChecked = true;
            chkAppend.IsChecked = false;
            chkULA.IsChecked = false;
            chkULA.IsEnabled = false;
            chkGrayscale.IsChecked = false;
            chkGrayscale.IsEnabled = false;
            Convertir();
        }

        #endregion


        #region Conversion options

        private void ChkUseCurrent_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool useCurrent = chkUseCurrent.IsChecked == true;
            chkULA.IsEnabled = !useCurrent;
            chkGrayscale.IsEnabled = !useCurrent;
            Convertir();
        }


        private void ChkAppend_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool append = chkAppend.IsChecked == true;
            chkULA.IsEnabled = append;
            chkGrayscale.IsEnabled = append;
            Convertir();
        }


        private void ChkGrayscale_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Convertir();
        }

        private void ChkULA_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Convertir();
        }


        private void BtnRefresh_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Convertir();
        }


        private void BtnSaveImage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[2] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };
            fileTypes[1] = new FilePickerFileType("Portable Nextwork Graphics (*.png)") { Patterns = new[] { "*.png" } };
            fileTypes[0] = new FilePickerFileType("Next image (*.nxi)") { Patterns = new[] { "*.nxi" } };

            var select = StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select file to save image...",
                ShowOverwritePrompt = true,
                DefaultExtension = "*.nxi",
                FileTypeChoices = fileTypes
            }).Result;

            if (select != null)
            {
                var fileName = select.Path.LocalPath;
                var extension = System.IO.Path.GetExtension(fileName);
                bool error = false;

                switch (extension)
                {
                    case ".png":
                        if (!imgConverted.SaveImageAsPng(select.Path.LocalPath))
                        {
                            error = true;
                        }
                        break;
                    case ".nxi":
                        if (!SaveAsNXI(fileName))
                        {
                            error = true;
                        }
                        break;
                    default:
                        {
                            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                            {
                                ButtonDefinitions = ButtonEnum.Ok,
                                ContentTitle = "SAVE ERROR",
                                ContentMessage = "Format not supported.",
                                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                                WindowIcon = ((Window)this.VisualRoot).Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                            box.ShowAsPopupAsync(this);
                        }
                        return;
                }

                if (error)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "SAVE ERROR",
                        ContentMessage = "An error occurred while saving the file.",
                        Icon = MsBox.Avalonia.Enums.Icon.Warning,
                        WindowIcon = ((Window)this.VisualRoot).Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                    box.ShowAsPopupAsync(this);
                }
            }
        }


        private bool SaveAsNXI(string fileName)
        {
            try
            {
                var data = new List<byte>();
                for (int y = 0; y < 192; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        data.Add((byte)(pixels[x, y]));
                    }
                }
                if (cmbMode.SelectedIndex == 0)
                {
                    foreach (var color in palette)
                    {
                        int r = color.Red / 36;
                        int g = color.Green / 36;
                        int b = color.Blue / 36;
                        byte d1 = (byte)(((r & 0x07) << 5) | ((g & 0x07) << 2) | ((b & 0x7) >> 1));
                        byte d2 = (byte)(b & 0x1);
                        if (color.HasPriority)
                        {
                            d2 = (byte)(d2 | 0x80);
                        }
                        data.Add(d1);
                        data.Add(d2);
                    }
                }
                return ServiceLayer.Files_SaveFileData(fileName, data.ToArray());
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion


        #region Crear paleta de 9 bits

        private static byte[] paletteDefRG = { 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff };

        private void CrearPaletaGPL9bits()
        {
            var sw = new StreamWriter("c:\\Temp\\Duefectu_9bit_work.gpl", false);
            int i = 0;
            sw.WriteLine("GIMP Palette");
            sw.WriteLine("Name: 9 bit palette for Next");
            sw.WriteLine("#By Duefectu");
            // Escala de grises
            for (int n = 0; n < 8; n++)
            {
                var s = paletteDefRG[n].ToString().PadLeft(3, ' ');
                sw.WriteLine($"{s} {s} {s}\tGrayScale {n}");
                i++;
            }

            // Colores ordenados por rojo
            if (false)
            {
                for (int b = 0; b < 8; b++)
                {
                    for (int g = 0; g < 8; g++)
                    {
                        for (int r = 0; r < 8; r++)
                        {
                            sw.WriteLine($"{paletteDefRG[r].ToString().PadLeft(3, ' ')} {paletteDefRG[g].ToString().PadLeft(3, ' ')} {paletteDefRG[b].ToString().PadLeft(3, ' ')}\ti:{i},r:{r},g:{g},b:{b}");
                            i++;
                        }
                    }
                }
            }

            // Espacio en negro
            for (int n = 0; n < 8; n++)
            {
                var s = "  0";
                sw.WriteLine($"{s} {s} {s}\tGrayScale {n}");
                i++;
            }

            // Rojo 1
            for (int n = 0; n < 8; n++)
            {
                var s = paletteDefRG[n].ToString().PadLeft(3, ' ');
                sw.WriteLine($"{s}   0   0\tGrayScale {n}");
                i++;
            }

            // Rojo 2
            for (int n = 0; n < 8; n++)
            {
                var s = paletteDefRG[n].ToString().PadLeft(3, ' ');
                sw.WriteLine($"255 {s} {s}\tGrayScale {n}");
                i++;
            }

            // Rojo 3
            for (int n = 0; n < 8; n++)
            {
                string c1 = "";
                string c2 = "";
                string c3 = "";
                c1 = paletteDefRG[n].ToString().PadLeft(3, ' ');
                if (n > 0)
                {
                    c2 = paletteDefRG[n - 1].ToString().PadLeft(3, ' ');
                }
                if (n > 1)
                {
                    c3 = paletteDefRG[n - 2].ToString().PadLeft(3, ' ');
                }
                sw.WriteLine($"255 {c1} {c2}\tGrayScale {n}");
                i++;
            }

            sw.Close();
        }

        #endregion
    }
}
