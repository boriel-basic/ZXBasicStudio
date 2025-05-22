using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ZXBasicStudio.Classes;
using System.Linq;
using System.Text;
using System.IO;
using ZXBasicStudio.Common;

namespace ZXBasicStudio.Dialogs
{
    public partial class ZXBuildSettingsDialog : ZXWindowBase
    {
        ZXBuildSettings _settings = new ZXBuildSettings();
        public ZXBuildSettings Settings { get { return _settings; } set { _settings = value; UpdateUI(); } }


        public ZXBuildSettingsDialog()
        {
            InitializeComponent();
            btnAccept.Click += BtnAccept_Click;
            btnCancel.Click += BtnCancel_Click;
            ckSinclair.IsCheckedChanged += CkSinclair_IsCheckedChanged;

            txtFile.TextChanged += TextBox_TextChanged;
            txtDefines.TextChanged += TextBox_TextChanged;
            ckBreak.Click += CheckBox_Click;
            ckCase.Click += CheckBox_Click;
            ckExplicit.Click += CheckBox_Click;
            ckHeaderless.Click += CheckBox_Click;
            ckNext.Click += CheckBox_Click;
            ckSinclair.Click += CheckBox_Click;
            ckStrict.Click += CheckBox_Click;
            cbArrayBase.SelectionChanged += ComboBox_SelectionChanged;
            cbOptimization.SelectionChanged += ComboBox_SelectionChanged;
            cbStringBase.SelectionChanged += ComboBox_SelectionChanged;

            nudOrg.ValueChanged += NumericUpDown_ValueChanged;
            nudHeap.ValueChanged += NumericUpDown_ValueChanged;

            chkPrebuild.Click += ChkPrebuild_Click;
            chkCompiler.Click += ChkCompiler_Click;
            chkPostbuild.Click += ChkPostbuild_Click;
            chkEmulator.Click += ChkEmulator_Click;

            btnPrebuild.Tapped += BtnPrebuild_Tapped;
            btnPostbuild.Tapped += BtnPostbuild_Tapped;
            btnEmulator.Tapped += BtnEmulator_Tapped;
        }

        private void NumericUpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            UpdateCustomBuilder();
        }

        private void ChkPrebuild_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private void ChkCompiler_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private void ChkPostbuild_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private void ChkEmulator_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private async void BtnPrebuild_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[0] = new FilePickerFileType("Batch/Shell files (*.bat, *.sh)") { Patterns = new[] { "*.bat", "*.sh" } };
            fileTypes[1] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };
            fileTypes[2] = new FilePickerFileType("Executable files (*.exe)") { Patterns = new[] { "*.exe" } };

            var select = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                FileTypeFilter = fileTypes.ToArray(),
                Title = "Select pre-build file...",
            });

            if (select != null && select.Count() != 0)
            {
                var fileName = Path.GetFullPath(select[0].Path.LocalPath);
                txtPreBuild.Text = fileName;
            }
        }


        private async void BtnPostbuild_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[0] = new FilePickerFileType("Batch/Shell files (*.bat, *.sh)") { Patterns = new[] { "*.bat", "*.sh" } };
            fileTypes[1] = new FilePickerFileType("Executable files (*.exe)") { Patterns = new[] { "*.exe" } };
            fileTypes[2] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };

            var select = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                FileTypeFilter = fileTypes.ToArray(),
                Title = "Select post-build file...",
            });

            if (select != null && select.Count() != 0)
            {
                var fileName = Path.GetFullPath(select[0].Path.LocalPath);
                txtPostbuild.Text = fileName;
            }
        }

        private async void BtnEmulator_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var fileTypes = new FilePickerFileType[3];
            fileTypes[0] = new FilePickerFileType("Batch/Shell files (*.bat, *.sh)") { Patterns = new[] { "*.bat", "*.sh" } };
            fileTypes[1] = new FilePickerFileType("Executable files (*.exe)") { Patterns = new[] { "*.exe" } };
            fileTypes[2] = new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*", "*.*" } };

            var select = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                FileTypeFilter = fileTypes.ToArray(),
                Title = "Select emulator launcher file...",
            });

            if (select != null && select.Count() != 0)
            {
                var fileName = Path.GetFullPath(select[0].Path.LocalPath);
                txtEmulator.Text = fileName;
            }
        }

        private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateCustomBuilder();
        }

        private void CheckBox_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            UpdateCustomBuilder();
        }


        private void CkSinclair_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if ((ckSinclair.IsChecked ?? false) == true)
            {
                cbArrayBase.IsEnabled = false;
                cbStringBase.IsEnabled = false;
                ckCase.IsEnabled = false;

                cbArrayBase.SelectedIndex = 1;
                cbStringBase.SelectedIndex = 1;
                ckCase.IsChecked = true;
            }
            else
            {
                cbArrayBase.IsEnabled = true;
                cbStringBase.IsEnabled = true;
                ckCase.IsEnabled = true;
            }
            UpdateCustomBuilder();
        }


        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close(false);
        }

        private void BtnAccept_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateSettings();
            this.Close(true);
        }


        private void UpdateUI()
        {
            if (_settings != null)
            {
                txtFile.Text = _settings.MainFile ?? "";
                cbOptimization.SelectedIndex = _settings.OptimizationLevel ?? -1;
                nudOrg.Value = _settings.Origin ?? 32768;

                ckSinclair.IsChecked = _settings.SinclairMode;
                if (!_settings.SinclairMode)
                {
                    cbArrayBase.SelectedIndex = _settings.ArrayBase ?? -1;
                    cbStringBase.SelectedIndex = _settings.StringBase ?? -1;
                    ckCase.IsChecked = _settings.IgnoreCase;
                }

                nudHeap.Value = _settings.HeapSize ?? 4768;
                ckBreak.IsChecked = _settings.EnableBreak;
                ckExplicit.IsChecked = _settings.Explicit;
                txtDefines.Text = _settings.Defines == null || _settings.Defines.Length == 0 ? "" : string.Join(", ", _settings.Defines);
                ckStrict.IsChecked = _settings.Strict;
                ckHeaderless.IsChecked = _settings.Headerless;
                ckNext.IsChecked = _settings.NextMode;

                chkPrebuild.IsChecked = _settings.PreBuild;
                txtPreBuild.Text = _settings.PreBuildValue.ToStringNoNull();
                chkCompiler.IsChecked = _settings.CustomCompiler;
                txtCompiler.Text = _settings.CustomCompilerValue.ToStringNoNull();
                chkPostbuild.IsChecked = _settings.PostBuild;
                txtPostbuild.Text = _settings.PostBuildValue.ToStringNoNull();
                chkEmulator.IsChecked = _settings.ExternalEmulator;
                txtEmulator.Text = _settings.ExternalEmuladorValue.ToStringNoNull();
            }
            else
            {
                txtFile.Text = "";
                cbOptimization.SelectedIndex = -1;
                nudOrg.Value = 32768;
                cbArrayBase.SelectedIndex = -1;
                cbStringBase.SelectedIndex = -1;
                ckSinclair.IsChecked = false;
                nudHeap.Value = 4768;
                ckBreak.IsChecked = false;
                ckExplicit.IsChecked = false;
                txtDefines.Text = "";
                ckCase.IsChecked = false;
                ckStrict.IsChecked = false;
                ckHeaderless.IsChecked = false;
                ckNext.IsChecked = false;

                chkPrebuild.IsChecked = false;
                txtPreBuild.Text = "";
                chkCompiler.IsChecked = false;
                txtPreBuild.Text = "";
                chkPostbuild.IsChecked = false;
                txtPostbuild.Text = "";
                chkEmulator.IsChecked = false;
                txtEmulator.Text = "";
            }
            UpdateCustomBuilder();
        }


        private void UpdateSettings()
        {
            if (_settings == null)
                _settings = new ZXBuildSettings();

            _settings.MainFile = string.IsNullOrWhiteSpace(txtFile.Text) ? null : txtFile.Text;
            _settings.OptimizationLevel = cbOptimization.SelectedIndex == -1 ? null : cbOptimization.SelectedIndex;
            _settings.Origin = nudOrg.Value == null || nudOrg.Value == 32768 ? null : (ushort)nudOrg.Value;
            _settings.ArrayBase = cbArrayBase.SelectedIndex == -1 ? null : cbArrayBase.SelectedIndex;
            _settings.StringBase = cbStringBase.SelectedIndex == -1 ? null : cbStringBase.SelectedIndex;
            _settings.SinclairMode = ckSinclair.IsChecked ?? false;
            _settings.HeapSize = (int?)(nudHeap.Value == 4768 ? null : nudHeap.Value);
            _settings.EnableBreak = ckBreak.IsChecked ?? false;
            _settings.Explicit = ckExplicit.IsChecked ?? false;
            _settings.Defines = string.IsNullOrWhiteSpace(txtDefines.Text) ? null : txtDefines.Text.Split(",").Select(s => s.Trim()).ToArray();
            _settings.IgnoreCase = ckCase.IsChecked ?? false;
            _settings.Strict = ckStrict.IsChecked ?? false;
            _settings.Headerless = ckHeaderless.IsChecked ?? false;
            _settings.NextMode = ckNext.IsChecked ?? false;

            _settings.PreBuild = chkPrebuild.IsChecked == true;
            _settings.PreBuildValue = txtPreBuild.Text.ToStringNoNull();
            _settings.CustomCompiler = chkCompiler.IsChecked == true;
            _settings.CustomCompilerValue = txtCompiler.Text.ToStringNoNull();
            _settings.PostBuild = chkPostbuild.IsChecked == true;
            _settings.PostBuildValue = txtPostbuild.Text.ToStringNoNull();
            _settings.ExternalEmulator = chkEmulator.IsChecked == true;
            _settings.ExternalEmuladorValue = txtEmulator.Text.ToStringNoNull();
        }


        private void UpdateCustomBuilder()
        {
            grdPrebuild.IsEnabled = chkPrebuild.IsChecked == true;
            grdCompiler.IsEnabled = chkCompiler.IsChecked == true;
            grdPostbuild.IsEnabled = chkPostbuild.IsChecked == true;
            grdEmulator.IsEnabled = chkEmulator.IsChecked == true;

            if (chkCompiler.IsChecked == true)
            {
                return;
            }

            var sb = new StringBuilder();
            //sb.Append($"zxbc {txtFile.Text}");
            sb.Append($" -O {cbOptimization.SelectedIndex}");
            sb.Append($" -S {nudOrg.Text}");
            if (cbArrayBase.SelectedIndex == 1)
            {
                sb.Append(" --array-base 1");
            }
            if (cbStringBase.SelectedIndex == 1)
            {
                sb.Append(" --string-base 1");
            }
            sb.Append($" -H {nudHeap.Text}");
            if (ckSinclair.IsChecked == true)
            {
                sb.Append(" -Z");
            }
            if (ckBreak.IsChecked == true)
            {
                sb.Append(" --enable-break");
            }
            if (ckExplicit.IsChecked == true)
            {
                sb.Append(" --explicit");
            }
            if (ckCase.IsChecked == true)
            {
                sb.Append(" -i");
            }
            if (ckStrict.IsChecked == true)
            {
                sb.Append(" --strict");
            }
            if (ckHeaderless.IsChecked == true)
            {
                sb.Append(" --headerless");
            }
            if (ckNext.IsChecked == true)
            {
                sb.Append(" --zxnext --arch=zxnext");
            }
            if (!string.IsNullOrEmpty(txtDefines.Text))
            {
                var defines = txtDefines.Text.Split(",").Select(s => s.Trim()).ToArray();
                foreach (var define in defines)
                {
                    sb.Append($" -D {define}");
                }
            }

            txtCompiler.Text = sb.ToString().Trim();
        }
    }
}
