using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;

//using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ZXBasicStudio.Classes;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.NextDows.neg;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentEditors.ZXMaps.Log;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;
using ZXBasicStudio.DocumentEditors.ZXTextEditor.Classes.Folding;
using ZXBasicStudio.DocumentModel.Classes;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class MapFileSelectorControl : UserControl
    {
        public ZXMapsMap Map
        {
            get
            {
                return _Map;
            }
            set
            {
                _Map = value;
                Refresh();
            }
        }

        private ZXMapsMap _Map = null;

        private int CurrentTap = 0;
        private Action<MapFileSelectorControl, string> Comando = null;
        private bool comboUpdating = false;


        public MapFileSelectorControl()
        {
            InitializeComponent();
        }


        public bool Initialize(ZXMapsMap map, Action<MapFileSelectorControl, string> callBackComando)
        {
            this._Map = map;
            this.Comando = callBackComando;

            FillCombo();
            return true;
        }


        public bool Initialize(string mapName, Action<MapFileSelectorControl, string> callBackComando)
        {
            this.Comando = callBackComando;
            if (!string.IsNullOrEmpty(mapName))
            {
                Map = ServiceLayer_Maps.Maps_LoadMapByName(mapName);
            }
            FillCombo();
            return true;
        }


        public void Refresh()
        {

        }


        private void FillCombo()
        {
            try
            {
                if (comboUpdating)
                {
                    return;
                }
                comboUpdating = true;

                var files = Directory.GetFiles(ZXProjectManager.Current.ProjectPath, "*.zxmap").
                    Select(Path.GetFileNameWithoutExtension).
                    OrderBy(f => f).
                    ToList();
                var sel = cmbFileName.SelectedValue;
                cmbFileName.ItemsSource = null;
                cmbFileName.Items.Clear();

                var numFiles = files.Count();
                if (numFiles == 0)
                {
                    files.Add("<Create a zxmap file to edit>");
                    cmbFileName.ItemsSource = files;
                    cmbFileName.SelectedIndex = 0;
                }
                else
                {
                    cmbFileName.ItemsSource = files;
                    if (sel == null)
                    {
                        cmbFileName.SelectedValue = Map?.Name;
                    }
                    else
                    {
                        cmbFileName.SelectedValue = sel;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Tratar excepciones
            }
            finally
            {
                comboUpdating = false;
            }
        }


        private void cmbFileName_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                var name = cmbFileName.Text;
                Map = ServiceLayer_Maps.Maps_LoadMapByName(name);
                Comando?.Invoke(this, "SELECTED");
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error loading file {FileName}: {ex.Message}");
            }

        }
    }
}