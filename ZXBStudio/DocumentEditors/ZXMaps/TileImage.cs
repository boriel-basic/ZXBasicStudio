using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public class TileImage : IZXBitmap, IDisposable
    {
        #region Private fields
        private WriteableBitmap? bitmap;
        #endregion

        #region Public properties

        public bool IsEmpty { get; private set; }
        public bool ViewAttributes { get; set; } = true;

        #endregion

        #region Constructors

        public TileImage()
        {
            bitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(72, 72), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);

            Clear(Colors.White);
        }


        public TileImage(ZXMapsTile Tile, int FrameNumber)
        {
            bitmap = new WriteableBitmap(new PixelSize(Tile.Width, Tile.Height), new Vector(72, 72), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);

            RenderTile(Tile, FrameNumber);
        }

        #endregion

        #region Public functions

        public unsafe void Clear(Color ClearColor)
        {
            if (bitmap == null) //disposed
                return;

            using var lockData = bitmap.Lock();
            uint* data = (uint*)lockData.Address;
            uint color = ToRgba(ClearColor);

            for (int y = 0; y < bitmap.PixelSize.Height; y++)
            {
                for (int x = 0; x < bitmap.PixelSize.Width; x++)
                {
                    data[y * lockData.RowBytes / 4 + x] = color;
                }

            }

            IsEmpty = true;
        }


        public unsafe void RenderTile(ZXMapsTile Tile, int FrameNumber)
        {
            try
            {
                if (Tile == null)
                {
                    return;
                }
                if (bitmap == null) //disposed
                    throw new ObjectDisposedException("ZXTileImage");

                if (bitmap.PixelSize.Width != Tile.Width || bitmap.PixelSize.Height != Tile.Height)
                {
                    bitmap.Dispose();
                    bitmap = new WriteableBitmap(new PixelSize(Tile.Width, Tile.Height), new Vector(72,72), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);
                }

                using var lockData = bitmap.Lock();
                uint* data = (uint*)lockData.Address;

                var frame = Tile.Patterns[FrameNumber];
                int index = 0;

                for (int y = 0; y < Tile.Height; y++)
                {
                    for (int x = 0; x < Tile.Width; x++)
                    {
                        int colorIndex = frame.RawData[index++];

                        PaletteColor color;

                        switch (Tile.GraphicMode)
                        {
                            case GraphicsModes.ZXSpectrum:
                                {
                                    var attr = GetAttribute(Tile, frame, x, y);
                                    if (colorIndex == 0)
                                    {
                                        if (ViewAttributes)
                                        {
                                            color = Tile.Palette[attr.Paper];                                            
                                        }
                                        else
                                        {
                                            color = Tile.Palette[7];
                                        }
                                    }
                                    else
                                    {
                                        if(ViewAttributes)
                                        {
                                            color = Tile.Palette[attr.Ink];
                                        }
                                        else
                                        {
                                            color = Tile.Palette[0];
                                        }
                                    }
                                }
                                break;
                            case GraphicsModes.Monochrome:
                                if (colorIndex > Tile.Palette.Length - 1)
                                {
                                    colorIndex = Tile.Palette.Length - 1;
                                }
                                color = Tile.Palette[colorIndex];
                                break;
                            default:
                                color = new PaletteColor { Red = 0xFF, Green = 0xFF, Blue = 0xFF };
                                break;
                        }

                        data[y * lockData.RowBytes / 4 + x] = ToRgba(color);

                    }
                }

                IsEmpty = false;
            } catch(Exception ex)
            {

            }
        }

        #endregion

        #region Private functions

        uint ToRgba(PaletteColor Color)
        {
            return (uint)((255 << 24) | (Color.Blue << 16) | (Color.Green << 8) | Color.Red);
        }
        uint ToRgba(Color Color)
        {
            return (uint)((255 << 24) | (Color.B << 16) | (Color.G << 8) | Color.R);
        }
        private AttributeColor GetAttribute(ZXMapsTile Tile, Pattern Pattern, int X, int Y)
        {
            int cW = Tile.Width / 8;
            int cX = X / 8;
            int cY = Y / 8;
            int dir = (cY * cW) + cX;
            if (Pattern.Attributes == null)
            {
                Pattern.Attributes = new AttributeColor[(Tile.Width + Tile.Height) / 8];
                for (int n = 0; n < Pattern.Attributes.Length; n++)
                {
                    Pattern.Attributes[n] = new AttributeColor()
                    {
                        Attribute = 56  // Paper 7, ink 0
                    };
                }
            }
            if (dir > Pattern.Attributes.Length)
            {
                return new AttributeColor()
                {
                    Attribute = 56  // Paper 7, ink 0
                };
            }
            return Pattern.Attributes[dir];
        }

        #endregion

        #region IZXBitmap implementation
        public Size Size
        {
            get
            {
                return bitmap.Size;
            }
        }


        public PixelSize PixelSize
        {
            get
            {
                return bitmap.PixelSize;
            }
        }


        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            ((IImage)bitmap).Draw(context, sourceRect, destRect);
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (bitmap == null)
                return;

            bitmap.Dispose();
            bitmap = null;
            IsEmpty = true;
        }

        #endregion
    }
}
