using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class TilePropertiesControl : UserControl
    {
        #region Public properties

        /// <summary>
        /// Tile data
        /// </summary>
        public ZXMapsTiles TileData
        {
            get
            {
                return _TileData;
            }
            set
            {
                _TileData = value;
                IsSelected = true;
            }
        }

        /// <summary>
        /// CallBack for commands: "SIZE", "MODE", "NAME"
        /// </summary>
        public Action<TilePropertiesControl, string> CallBackCommand { get; set; }

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
                _IsSelected = value;
                Refresh();
            }
        }


        public int PrimaryColor = 1;
        public int SecondaryColor = 0;

        #endregion


        #region Private fields

        private bool _IsSelected = false;
        private bool _SettingsChanged = false;
        private bool refreshing = false;
        private bool newTile = true;
        private ZXMapsTiles _TileData = null;

        #endregion


        #region Constructor and public methods

        public TilePropertiesControl()
        {
            InitializeComponent();
            Refresh();
        }


        /// <summary>
        /// Initializes the control
        /// </summary>
        /// <param name="TileData">Data of the Tile, if is null, the "Add" icon is visible and no properties are shown</param>
        /// <param name="callBackCommand">CallBak for actions command, line "ADD", "CLONE", "DELETE" or "SELECTED"</param>
        /// <returns></returns>
        public bool Initialize(ZXMapsTiles TileData, Action<TilePropertiesControl, string> callBackCommand)
        {
            this.TileData = TileData;
            this.CallBackCommand = callBackCommand;

            txtName.TextChanged += TxtName_TextChanged;
            cmbMode.SelectionChanged += CmbMode_SelectionChanged;
            txtWidth.ValueChanged += TxtWidth_ValueChanged;
            txtHeight.ValueChanged += TxtHeight_ValueChanged;

            _SettingsChanged = false;

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
                if (TileData == null)
                {
                    pnlProperties.IsVisible = false;
                    return;
                }
                else
                {
                    pnlProperties.IsVisible = true;
                }

                txtName.Text = TileData.Name;
                txtWidth.Text = TileData.Width.ToStringNoNull();
                txtHeight.Text = TileData.Height.ToStringNoNull();
                cmbMode.SelectedIndex = (int)TileData.GraphicMode;

                txtHeight.Value = TileData.Height;
                txtName.Text = TileData.Name;
                txtWidth.Value = TileData.Width;
                cmbMode.SelectedIndex = (byte)TileData.GraphicMode;

            }
            catch { }
            finally
            {
                refreshing = false;
            }
        }

        #endregion


        #region Private methods



        #endregion


        #region Button events       

        private void CmbMode_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            TileData.GraphicMode = (ZXGraphics.neg.GraphicsModes)cmbMode.SelectedIndex;
            CallBackCommand(this, "MODE");
        }


        private void TxtWidth_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            TileData.Width = txtWidth.Value.ToInteger();
            CallBackCommand(this, "SIZE");
        }


        private void TxtHeight_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            TileData.Height = txtHeight.Value.ToInteger();
            CallBackCommand(this, "SIZE");
        }


        private void TxtName_TextChanged(object? sender, TextChangedEventArgs e)
        {
            TileData.Name = txtName.Text.ToStringNoNull();
            CallBackCommand(this, "NAME");
        }
    
        #endregion

    }
}
