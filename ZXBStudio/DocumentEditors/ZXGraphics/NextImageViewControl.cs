using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.Common;
using SixLabors.ImageSharp.Formats.Bmp;

namespace ZXBasicStudio.DocumentEditors.ZXGraphics
{
    /// <summary>
    /// Control to show and manipulate imported image
    /// Fixed size to 400x400
    /// </summary>
    public class NextImageViewControl : Control
    {
        /// <summary>
        /// Zoom factor
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
                this.InvalidateVisual();
            }
        }
        private int _Zoom = 1;

        /// <summary>
        /// Import image data
        /// </summary>
        public SixLabors.ImageSharp.Image<Rgba32> imageData = null;

        public NextImageViewControl()
        {
        }


        public async void LoadImage(IStorageFile file)
        {
            try
            {
                await using (var stream = await file.OpenReadAsync())
                {
                    imageData = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
                }
                if (imageData.Width > 320)
                {
                    this.Width = 320;
                }
                else
                {
                    this.Width = imageData.Width;
                }
                if (this.imageData.Height > 256)
                {
                    this.Height = 256;
                }
                else
                {
                    this.Height = imageData.Height;
                }

            }
            catch (Exception ex)
            {
            }
        }


        public void Refresh()
        {
            this.InvalidateVisual();
        }


        public override void Render(DrawingContext context)
        {
            try
            {
                base.Render(context);

                // Image
                if (imageData == null)
                {
                    return;
                }
                int iw = imageData.Size.Width;
                int ih = imageData.Size.Height;
                int cw = 0;
                int ch = 0;
                int z = _Zoom;
                int maxWidth = (int)this.Width;
                int maxHeight = (int)this.Height;

                int yd = 0;
                for (int y = 0; y < ih; y += z)
                {
                    int xd = 0;
                    for (int x = 0; x < iw; x += z)
                    {
                        if (xd >= 0 && xd < iw &&
                            yd >= 0 && yd < ih)
                        {
                            try
                            {
                                int xx = x;
                                int yy = y;

                                if ((xx + z) > iw)
                                {
                                    cw = iw - xx;
                                }
                                else
                                {
                                    cw = z;
                                }
                                if ((yy + z) > ih)
                                {
                                    ch = ih - yy;
                                }
                                else
                                {
                                    ch = z;
                                }
                                if (xx >= 0 && xx <= maxWidth && yy >= 0 && yy <= maxHeight)
                                {
                                    Rect r = new Rect(xx, yy, cw, ch);
                                    var pixel = imageData[xd, yd];
                                    var brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(
                                        255, pixel.R, pixel.G, pixel.B));
                                    context.FillRectangle(brush, r);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        xd++;
                    }
                    yd++;
                }

            }
            catch (Exception ex)
            {

            }
        }


        public void SetImageData(int[,] pixels, PaletteColor[] palette, int w, int h)
        {
            imageData = new Image<Rgba32>(w, h);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var p = palette[pixels[x, y]];
                    imageData[x, y] = new Rgba32(p.Red, p.Green, p.Blue);
                }
            }
            this.InvalidateArrange();
        }


        public bool SaveImageAsPng(string filename)
        {
            try
            {
                imageData.SaveAsPng(filename);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
