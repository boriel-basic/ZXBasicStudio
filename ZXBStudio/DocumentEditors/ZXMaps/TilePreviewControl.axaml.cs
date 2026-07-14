using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class TilePreviewControl : UserControl, IDisposable
    {
        #region Public properties

        /// <summary>
        /// Tile data
        /// </summary>
        public ZXMapsTile? TileData { get; set; }
        public int Zoom { get; set; } = 4;

        #endregion

        #region Private fields

        private int frameNumber = 0;
        private int speed = 1;
        private DispatcherTimer tmr;
        private Color emptyColor = new Color(255, 0x28, 0x28, 0x28);
        TileImage aspect = new TileImage();

        /// <summary>
        /// Speeds in milliseconds
        /// </summary>
        private int[] speeds = new int[] { 600000, 1000, 500, 250, 200, 125, 100, 66, 50, 20 };

        #endregion

        public TilePreviewControl()
        {
            tmr = new DispatcherTimer(TimeSpan.FromMilliseconds(speeds[2]), DispatcherPriority.Normal, Refresh);
            tmr.Stop();
            aspect.Clear(emptyColor);
            InitializeComponent();
            imgPreview.Source = aspect;
            imgPreview.Width = aspect.Size.Width;
            imgPreview.Height = aspect.Size.Height;
        }

        /// <summary>
        /// Initializes the control
        /// </summary>
        /// <param name="TileData">Data of the Tile, if is null, the "Add" icon is visible and no properties are shown</param>
        /// <param name="callBackCommand">CallBak for actions command, line "ADD", "CLONE", "DELETE" or "SELECTED"</param>
        /// <returns></returns>
        public bool Initialize(ZXMapsTile TileData)
        {
            this.TileData = TileData;

            //this.cmbSpeed.SelectionChanged += CmbSpeed_SelectionChanged;
            tmr.Interval = TimeSpan.FromMilliseconds(speeds[speed]);

            return true;
        }

        public void Start()
        {
            tmr.Start();
        }

        public void Stop()
        {
            tmr.Stop();
        }

        //private void CmbSpeed_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        //{
            //var s = cmbSpeed.SelectedIndex.ToInteger();
            //if (s < 0 || s >= speeds.Length)
            //{
            //    s = 2;
            //}
            //speed = s;
            //tmr.Interval = TimeSpan.FromMilliseconds(speeds[speed]);
        //}

        public void Refresh(object? sender = null, EventArgs? e = null)
        {
            if (TileData == null || TileData.Patterns == null || TileData.Patterns.Count == 0)
            {
                // Delete background

                if (aspect.IsEmpty)
                    return;

                aspect.Clear(emptyColor);
                imgPreview.Width = aspect.Size.Width;
                imgPreview.Height = aspect.Size.Height;
                imgPreview.InvalidateVisual();
                return;
            }

            if (tmr == null)
            {
                tmr = new DispatcherTimer(TimeSpan.FromMilliseconds(speeds[speed]), DispatcherPriority.Normal, Refresh);
            }

            if (TileData.Masked)
            {
                frameNumber += 2;
            }
            else
            {
                frameNumber++;
            }

            if (frameNumber >= TileData.Frames)
            {
                frameNumber = 0;
            }

            imgPreview.Width = TileData.Width * Zoom;
            imgPreview.Height = TileData.Height * Zoom;

            aspect.RenderTile(TileData, frameNumber);
            imgPreview.InvalidateVisual();
        }

        public void Dispose()
        {
            Stop();
            aspect.Dispose();
            imgPreview.Source = null;
        }
    }
}
