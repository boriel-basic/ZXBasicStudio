using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class TilePatternControl : UserControl
    {
        #region Public properties

        /// <summary>
        /// Tile data
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
                IsSelected = true;
                //ApplySettings(false);
                Refresh();
            }
        }

        /// <summary>
        /// CallBack for commands: "ADD", "CLONE", "DELETE", "SELECT", "MODE", "SIZE"
        /// </summary>
        public Action<TilePatternControl, string> CallBackCommand { get; set; }

        /// <summary>
        /// True when then control is selected
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                    Refresh();
                }
            }
        }

        public bool InfoVisible
        {
            get
            {
                return _InfoVisible;
            }
            set
            {
                _InfoVisible = value;
                Refresh();
            }
        }

        /// <summary>
        /// The settings has changed
        /// </summary>
        public bool SettingsChanged
        {
            get
            {
                return _SettingsChanged;
            }
            set
            {
                _SettingsChanged = value;
                Refresh();
            }
        }

        #endregion


        #region Private fields

        private bool _IsSelected = false;
        private bool _SettingsChanged = false;
        private bool refreshing = false;
        private bool newTile = true;
        private ZXMapsTile _TileData = null;

        private int tileWidth = 16;
        private int tileHeight = 16;

        private bool _InfoVisible = false;

        #endregion


        #region Constructor and public methods

        public TilePatternControl()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Initializes the control
        /// </summary>
        /// <param name="TileData">Data of the Tile, if is null, the "Add" icon is visible and no properties are shown</param>
        /// <param name="callBackCommand">CallBak for actions command, line "ADD", "CLONE", "DELETE" or "SELECTED"</param>
        /// <returns></returns>
        public bool Initialize(ZXMapsTile TileData, Action<TilePatternControl, string> callBackCommand, int tileWidth, int tileHeight)
        {
            this.TileData = TileData;
            this.CallBackCommand = callBackCommand;
            this.tileWidth = tileWidth;
            this.tileHeight = tileHeight;

            this.PointerPressed += TilePropertiesControl_PointerPressed;

            btnNew.Tapped += BtnNew_Tapped;

            _SettingsChanged = false;
            newTile = true;

            Select();

            return true;
        }


        public void Refresh()
        {
            if (refreshing)
            {
                return;
            }
            refreshing = true;

            try
            {
                if (TileData != null)
                {
                    tileWidth = TileData.Width;
                    tileHeight = TileData.Height;
                }                

                var w = tileWidth * 4;
                var h = tileHeight * 4;
                var w4 = w + 4;
                var h4 = h + 4;

                if (this.Width != w4 || this.Height != h4)
                {
                    this.Width = w + 4;
                    this.Height = h + 4;
                    cnvPoints.Width = w;
                    cnvPoints.Height = h;                   
                }

                if (TileData == null)
                {
                    btnNew.IsVisible = true;
                    cnvPoints.IsVisible = false;
                    return;
                }                

                if (_IsSelected)
                {
                    brdMain.BorderBrush = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    brdMain.BorderBrush = new SolidColorBrush(Colors.Gray);
                }

                btnNew.IsVisible = false;

                cnvPoints.IsVisible = true;

                if (TileData.Patterns == null || TileData.Patterns.Count == 0)
                {
                    TileData.Patterns = new List<Pattern>();
                    TileData.Patterns.Add(new Pattern()
                    {
                        RawData = new int[tileHeight*tileHeight],
                        Id = 0,
                        Name = "",
                        Number = ""
                    });
                }
                if (TileData.Palette == null || TileData.Palette.Length == 0)
                {
                    TileData.Palette = ServiceLayer.GetPalette(TileData.GraphicMode);
                }

                // Delete background
                {
                    var r = new Rectangle();
                    r.Width = cnvPoints.Width;
                    r.Height = cnvPoints.Height;
                    r.Fill = new SolidColorBrush(new Color(255, 0x28, 0x28, 0x28));
                    cnvPoints.Children.Add(r);
                    Canvas.SetTop(r, 0);
                    Canvas.SetLeft(r, 0);
                }

                cnvPoints.Width = TileData.Width * 4;
                cnvPoints.Height = TileData.Height * 4;

                cnvPoints.Children.Clear();
                int index = 0;
                var frame = TileData.Patterns[0];
                for (int y = 0; y < TileData.Height; y++)
                {
                    for (int x = 0; x < TileData.Width; x++)
                    {
                        var colorIndex = frame.RawData[index];
                        var r = new Rectangle();
                        r.Width = 4;
                        r.Height = 4;
                        //var palette = TileData.Palette[p];

                        //r.Fill = new SolidColorBrush(new Color(255, palette.Red, palette.Green, palette.Blue));

                        switch (TileData.GraphicMode)
                        {
                            case GraphicsModes.ZXSpectrum:
                                {
                                    var attr = GetAttribute(frame, x, y);
                                    PaletteColor palette = null;
                                    if (colorIndex == 0)
                                    {
                                        palette = TileData.Palette[attr.Paper];
                                    }
                                    else
                                    {
                                        palette = TileData.Palette[attr.Ink];
                                    }
                                    r.Fill = new SolidColorBrush(new Color(255, palette.Red, palette.Green, palette.Blue));
                                }
                                break;
                            case GraphicsModes.Monochrome:
                            case GraphicsModes.Next:
                                {
                                    var palette = TileData.Palette[colorIndex];
                                    r.Fill = new SolidColorBrush(new Color(255, palette.Red, palette.Green, palette.Blue));
                                }
                                break;

                        }

                        cnvPoints.Children.Add(r);
                        Canvas.SetTop(r, y * 4);
                        Canvas.SetLeft(r, x * 4);
                        index++;
                    }
                }

                lblNumber.IsVisible = _InfoVisible;
                if (_InfoVisible)
                {
                    lblNumber.Text = TileData.Id.ToStringNoNull();
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                refreshing = false;
            }
        }


        private AttributeColor GetAttribute(Pattern pattern, int x, int y)
        {
            int cW = TileData.Width / 8;
            int cX = x / 8;
            int cY = y / 8;
            return pattern.Attributes[(cY * cW) + cX];
        }


        public void Select()
        {
            _IsSelected = true;
            Refresh();
            CallBackCommand(this, "SELECTED");
        }


        public void ApplySettings(bool askForApply)
        {
            /*
            if (_TileData == null)
            {
                return;
            }
            else
            {
                var sp = _TileData.Clonar<ZXMapsTile>();                

                if (sp.Width != _TileData.Width || sp.Height != _TileData.Height)
                {
                    if (!ServiceLayer.TileData_Resize(ref sp, _TileData.Width, _TileData.Height))
                    {
                        // TODO: Report error
                        return;
                    }
                }
                if (sp.GraphicMode != _TileData.GraphicMode)
                {
                    if (!ServiceLayer.TileData_ChangeMode(ref sp, _TileData.GraphicMode))
                    {
                        // TODO: Report error
                        return;
                    }
                }
                if (sp.Masked != _TileData.Masked)
                {
                    if (!ServiceLayer.TileData_ChangeMasked(ref sp, _TileData.Masked))
                    {
                        // TODO: Report error
                        return;
                    }
                }
                if (sp.Frames != _TileData.Frames)
                {
                    if (!ServiceLayer.TileData_ChangeFrames(ref sp, _TileData.Frames))
                    {
                        // TODO: Report error
                        return;
                    }
                }

                _TileData = sp;
                //CallBackCommand?.Invoke(this, "UPDATE");
                //Refresh();
                newTile = false;
                SettingsChanged = false;
            }
            */
        }

        #endregion


        #region Private methods

        private void AddNew()
        {
            var sp = new ZXMapsTile()
            {
                CurrentFrame = 0,
                DefaultColor = 0,
                Frames = 1,
                GraphicMode = GraphicsModes.Monochrome,
                Height = tileWidth,
                Id = -1,
                Masked = false,
                Name = "",
                Patterns = new List<Pattern>(),
                Width = tileHeight
            };
            sp.Palette = ServiceLayer.GetPalette(sp.GraphicMode);
            sp.Patterns.Add(CreatePattern());
            TileData = sp;
            _IsSelected = true;
            Refresh();

            CallBackCommand?.Invoke(this, "ADD");
        }


        private Pattern CreatePattern()
        {
            var pat = new Pattern()
            {
                Id = 0,
                Name = "",
                Number = "0",
                RawData = new int[tileWidth*tileHeight]
            };
            return pat;
        }


        #endregion


        #region Button events

        private void BtnNew_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            AddNew();
        }


        private void TilePropertiesControl_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            Select();
        }

        #endregion

    }
}
