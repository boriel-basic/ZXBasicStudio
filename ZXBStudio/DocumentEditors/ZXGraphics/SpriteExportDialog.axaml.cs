using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ZXBasicStudio.Classes;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentModel.Classes;
using ZXBasicStudio.DocumentModel.Interfaces;
using ZXBasicStudio.Extensions;
using ZXBasicStudio.IntegratedDocumentTypes.CodeDocuments.Basic;
using static System.Net.Mime.MediaTypeNames;

namespace ZXBasicStudio.DocumentEditors.ZXGraphics
{
    public partial class SpriteExportDialog : Window
    {
        private string fileName = "";
        private IEnumerable<Sprite> sprites = null;


        private FileTypeConfig fileType = null;
        private Pattern[] patterns = null;
        private ExportConfig exportConfig = null;


        public SpriteExportDialog()
        {
            InitializeComponent();

            btnCancel.Tapped += BtnCancel_Tapped;
            btnCopy.Tapped += BtnCopy_Tapped;
            btnExport.Tapped += BtnExport_Tapped;
            btnOutputFile.Tapped += BtnOutputFile_Tapped;
            cmbDataType.SelectionChanged += CmbDataType_SelectionChanged;
            cmbArrayBase.SelectionChanged += CmbArrayBase_SelectionChanged;
            chkAttr.Click += ChkAttr_Click;
            btnSave.Tapped += BtnSave_Tapped;
        }


        public bool Initialize(string fileName, IEnumerable<Sprite> spritesData)
        {
            this.fileName = fileName;
            this.sprites = spritesData;

            exportConfig = ServiceLayer.Export_GetConfigFile(fileName + ".zbs");
            if (exportConfig == null)
            {
                exportConfig = ServiceLayer.Export_Sprite_GetDefaultConfig(fileName);
            }

            this.cmbSelectExportType.Initialize(ExportType_Changed);
            cmbSelectExportType.InitializeSprite();

            txtOutputFile.Text = exportConfig.ExportFilePath;
            txtLabelName.Text = exportConfig.LabelName;
            chkAuto.IsChecked = exportConfig.AutoExport;
            cmbDataType.SelectedIndex = (int)exportConfig.ExportDataType;
            cmbArrayBase.SelectedIndex = exportConfig.ArrayBase.ToInteger();
            chkAttr.IsChecked = exportConfig.IncludeAttr;

            cmbSelectExportType.ExportType = exportConfig.ExportType;

            return true;
        }


        #region ExportOptions

        private void ExportType_Changed(ExportTypes exportType)
        {
            exportConfig.ExportType = exportType;
            Refresh();
        }


        private void Refresh()
        {
            grdOptions.IsVisible = true;

            chkAuto.IsVisible = true;

            lblOutputFile.IsVisible = true;
            txtOutputFile.IsVisible = true;
            btnOutputFile.IsVisible = true;

            lblLabelName.IsVisible = true;
            txtLabelName.IsVisible = true;

            lblArrayBase.IsVisible = true;
            cmbDataType.IsVisible = true;
            cmbArrayBase.IsVisible = true;

            bool canExport = false;
            if (sprites == null || sprites.Where(d => d != null && d.Export).Count() == 0)
            {
                txtError.IsVisible = true;
            }
            else
            {
                canExport = true;
                txtError.IsVisible = false;
            }

            switch (exportConfig.ExportType)
            {
                case ExportTypes.PutChars:
                    CreateExportPath(".bas");
                    if (canExport)
                    {
                        CreateExample_PutChars();
                    }
                    break;
                case ExportTypes.MaskedSprites:
                    CreateExportPath(".bas");
                    if (canExport)
                    {
                        CreateExample_MaskedSprites();
                    }
                    break;
                default:
                    grdOptions.IsVisible = false;
                    break;
            }
        }


        private void CreateExportPath(string extension)
        {
            if (string.IsNullOrEmpty(exportConfig.ExportFilePath))
            {
                txtOutputFile.Text = fileType.FileName + extension;
                var spName = Path.GetFileNameWithoutExtension(fileType.FileName.ToStringNoNull());
                txtLabelName.Text = spName;
                if (spName.Length > 10)
                {
                    spName = spName.Substring(0, 10);
                }
            }
        }


        #endregion


        #region Examples

        private void GetConfigFromUI()
        {
            if (exportConfig == null)
            {
                exportConfig = new ExportConfig();
            }
            exportConfig.ArrayBase = cmbArrayBase.SelectedIndex.ToInteger();
            exportConfig.AutoExport = chkAuto.IsChecked == true;
            exportConfig.ExportDataType = (ExportDataTypes)cmbDataType.SelectedIndex.ToInteger();
            exportConfig.ExportFilePath = txtOutputFile.Text.ToStringNoNull();
            exportConfig.ExportType = cmbSelectExportType.ExportType;
            exportConfig.LabelName = txtLabelName.Text.ToStringNoNull();
            exportConfig.ZXAddress = 0;
            exportConfig.ZXFileName = "";
            exportConfig.IncludeAttr = chkAttr.IsChecked == true;
        }

        private void CreateExample_PutChars()
        {
            if (sprites == null || sprites.Count() == 0)
            {
                txtCode.Text = "";
            }

            var sprite = sprites.ElementAt(0);
            var res = ExportManager.Export_Sprite_PutChars(exportConfig, sprites);
            if (res.StartsWith("ERROR:"))
            {
                txtError.Text = res;
                txtError.IsVisible = true;
                return;
            }
            else
            {
                txtError.IsVisible = false;
            }

            var sb = new StringBuilder();
            switch (exportConfig.ExportDataType)
            {
                case ExportDataTypes.DIM:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine(string.Format("' Can use: #INCLUDE \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine(res);
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");

                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2}{3}({4}))",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName,
                            sprite.Name.Replace(" ", "_"),
                            sprite.Frames == 1 ? "0" : "0,0"));
                        sb.AppendLine("");
                    }
                    break;

                case ExportDataTypes.ASM:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2}{3})",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName,
                            sprite.Name.Replace(" ", "_")));
                        sb.AppendLine("");
                        sb.AppendLine("' This section must not be executed");
                        sb.AppendLine(string.Format("' Can use: #INCLUDE \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine(res);
                    }
                    break;

                case ExportDataTypes.BIN:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2})",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName));
                        sb.AppendLine("");
                        sb.AppendLine("' This section must not be executed");
                        sb.AppendLine(string.Format(
                            "{0}:",
                            exportConfig.LabelName));
                        sb.AppendLine("ASM");
                        sb.AppendLine(string.Format("\tINCBIN \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine("END ASM");
                    }
                    break;

                case ExportDataTypes.TAP:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine("' Load .tap data ------------------------------------------");
                        sb.AppendLine("LOAD \"\" CODE");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2})",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName));
                        sb.AppendLine("");
                    }
                    break;
            }

            txtCode.Text = sb.ToString();
        }


        private void CreateExample_MaskedSprites()
        {
            if (sprites == null || sprites.Count() == 0)
            {
                txtCode.Text = "";
            }

            var sprite = sprites.ElementAt(0);
            var exportData = ExportManager.Export_Sprite_MaskedSprites(exportConfig, sprites);
            if (exportData.StartsWith("ERROR:"))
            {
                txtError.Text = exportData;
                txtError.IsVisible = true;
                return;
            }
            txtError.IsVisible = false;

            var sb = new StringBuilder();
            switch (exportConfig.ExportDataType)
            {
                case ExportDataTypes.DIM:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <retrace.bas>");
                        sb.AppendLine("#INCLUDE \"maskedsprites.bas\"");
                        sb.AppendLine("");
                        sb.AppendLine("'- Sprites definition -------------------------------------");
                        sb.AppendLine(string.Format("' Can use: #INCLUDE \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine(exportData);
                        sb.AppendLine("' - Init vars ---------------------------------------------");
                        sb.AppendLine("#define NumberofMaskedSprites   1");
                        sb.AppendLine("DIM sprDir, sprBackDir AS UInteger");
                        sb.AppendLine("");
                        sb.AppendLine("'- Initialise the sprite engine ---------------------------");
                        sb.AppendLine("InitMaskedSpritesFileSystem()");
                        sb.AppendLine("' Create sprite");
                        sb.AppendLine($"sprDir = RegisterSpriteImageInMSFS(@{exportConfig.LabelName}{sprite.Name.Replace(" ", "_")}(0))");
                        sb.AppendLine("' Save background for first time");
                        sb.AppendLine("sprBackDir = MaskedSpritesBackground(0)");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine("DIM n AS UByte");
                        sb.AppendLine("DO");
                        sb.AppendLine("    ' Sync with raytrace");
                        sb.AppendLine("    waitretrace");
                        sb.AppendLine("    ' Restore background");
                        sb.AppendLine("    RestoreBackground(100,52,sprBackDir)");
                        sb.AppendLine("    ");
                        sb.AppendLine("    ' Print a number");
                        sb.AppendLine("    PRINT AT 7,12;n;");
                        sb.AppendLine("    n = n + 1");
                        sb.AppendLine("    ' Draw sprite");
                        sb.AppendLine("    SaveBackgroundAndDrawSpriteRegisteredInMSFS(100,52,sprBackDir,sprDir)   ");
                        sb.AppendLine("LOOP");
                        sb.AppendLine("");
                    }
                    break;

                case ExportDataTypes.ASM:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <retrace.bas>");
                        sb.AppendLine("#INCLUDE \"maskedsprites.bas\"");
                        sb.AppendLine("");
                        sb.AppendLine("' - Init vars ---------------------------------------------");
                        sb.AppendLine("#define NumberofMaskedSprites   1");
                        sb.AppendLine("DIM sprDir, sprBackDir AS UInteger");
                        sb.AppendLine("");
                        sb.AppendLine("'- Initialise the sprite engine ---------------------------");
                        sb.AppendLine("InitMaskedSpritesFileSystem()");
                        sb.AppendLine("' Create sprite");
                        sb.AppendLine($"sprDir = RegisterSpriteImageInMSFS(@{exportConfig.LabelName}_{sprite.Name.Replace(" ", "_")})");
                        sb.AppendLine("' Save background for first time");
                        sb.AppendLine("sprBackDir = MaskedSpritesBackground(0)");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine("DIM n AS UByte");
                        sb.AppendLine("DO");
                        sb.AppendLine("    ' Sync with raytrace");
                        sb.AppendLine("    waitretrace");
                        sb.AppendLine("    ' Restore background");
                        sb.AppendLine("    RestoreBackground(100,52,sprBackDir)");
                        sb.AppendLine("    ");
                        sb.AppendLine("    ' Print a number");
                        sb.AppendLine("    PRINT AT 7,12;n;");
                        sb.AppendLine("    n = n + 1");
                        sb.AppendLine("    ' Draw sprite");
                        sb.AppendLine("    SaveBackgroundAndDrawSpriteRegisteredInMSFS(100,52,sprBackDir,sprDir)   ");
                        sb.AppendLine("LOOP");
                        sb.AppendLine("");
                        sb.AppendLine("' - This section must not be executed --------------------");
                        sb.AppendLine(string.Format("' Can use: #INCLUDE \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine(exportData);
                    }
                    break;

                case ExportDataTypes.BIN:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2})",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName));
                        sb.AppendLine("");
                        sb.AppendLine("' This section must not be executed");
                        sb.AppendLine(string.Format(
                            "{0}:",
                            exportConfig.LabelName));
                        sb.AppendLine("ASM");
                        sb.AppendLine(string.Format("\tINCBIN \"{0}\"",
                            Path.GetFileName(exportConfig.ExportFilePath)));
                        sb.AppendLine("END ASM");
                    }
                    break;

                case ExportDataTypes.TAP:
                    {
                        sb.AppendLine("'- Includes -----------------------------------------------");
                        sb.AppendLine("#INCLUDE <putchars.bas>");
                        sb.AppendLine("");
                        sb.AppendLine("' Load .tap data ------------------------------------------");
                        sb.AppendLine("LOAD \"\" CODE");
                        sb.AppendLine("");
                        sb.AppendLine("'- Draw sprite --------------------------------------------");
                        sb.AppendLine(string.Format(
                            "putChars(10,5,{0},{1},@{2})",
                            sprite.Width / 8,
                            sprite.Height / 8,
                            exportConfig.LabelName));
                        sb.AppendLine("");
                    }
                    break;
            }

            txtCode.Text = sb.ToString();
        }
        #endregion


        #region Export

        private void Export()
        {
            GetConfigFromUI();
            var em = new ExportManager();
            em.Initialize(FileTypes.Sprite);
            em.ExportSprites(exportConfig, sprites);
        }

        #endregion


        #region Buttons

        private void BtnSave_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            GetConfigFromUI();
            ServiceLayer.Export_SetConfigFile(fileName + ".zbs", exportConfig);
            Export();
            //this.Close();
        }

        private async void BtnOutputFile_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[2];
            fileTypes[1] = new FilePickerFileType("All files") { Patterns = new[] { "*", "*.*" } };
            fileTypes[0] = new FilePickerFileType("Sprite files") { Patterns = new[] { "*.spr" } };

            var select = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                ShowOverwritePrompt = true,
                FileTypeChoices = fileTypes,
                Title = "Select Export path...",
            });

            if (select != null)
            {
                txtOutputFile.Text = Path.GetFullPath(select.Path.LocalPath);
            }
        }


        private void BtnExport_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            GetConfigFromUI();
            Export();
            this.Close();
        }


        private void BtnCopy_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(txtCode.Text);
        }


        private void BtnCancel_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.Close();
        }



        private void CmbArrayBase_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var idx = cmbArrayBase.SelectedIndex;
            exportConfig.ArrayBase = idx;
            Refresh();
        }


        private void CmbDataType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var idx = cmbDataType.SelectedIndex;
            exportConfig.ExportDataType = (ExportDataTypes)idx;

            var ext = "";
            switch (exportConfig.ExportDataType)
            {
                case ExportDataTypes.ASM:
                case ExportDataTypes.DIM:
                    ext = ".bas";
                    break;
                case ExportDataTypes.BIN:
                    ext = ".bin";
                    break;
                case ExportDataTypes.TAP:
                    ext = ".tap";
                    break;
                default:
                    return;
            }
            txtOutputFile.Text = Path.Combine(Path.GetDirectoryName(txtOutputFile.Text), Path.GetFileNameWithoutExtension(txtOutputFile.Text) + ext);
            exportConfig.ExportFilePath = txtOutputFile.Text;
            Refresh();
        }


        private void ChkAttr_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            exportConfig.IncludeAttr = chkAttr.IsChecked.ToBoolean();
            Refresh();
        }

        #endregion
    }
}
