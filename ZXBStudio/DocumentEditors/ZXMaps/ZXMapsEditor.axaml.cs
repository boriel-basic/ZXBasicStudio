using Avalonia;
using Avalonia.Controls;
//using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.NextDows.neg;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentEditors.ZXTextEditor.Classes.Folding;
using ZXBasicStudio.DocumentModel.Classes;

namespace ZXBasicStudio.DocumentEditors.ZXMaps
{
    public partial class ZXMapsEditor : ZXDocumentEditorBase//, IObserver<AvaloniaPropertyChangedEventArgs>
    {
        #region Events

        public override event EventHandler? DocumentRestored;
        public override event EventHandler? DocumentModified;
        public override event EventHandler? DocumentSaved;
        public override event EventHandler? RequestSaveDocument;

        #endregion



        #region ZXDocumentBase properties

        public override string DocumentName
        {
            get
            {
                return Path.GetFileName(FileName);
            }
        }


        public override string DocumentPath
        {
            get
            {
                return Path.GetFullPath(FileName);
            }
        }

        public override bool Modified
        {
            get
            {
                return _Modified;
            }
        }

        private bool _Modified = false;

        protected virtual AbstractFoldingStrategy? foldingStrategy { get { return null; } }

        private Guid documentTypeId = Guid.Empty;

        #endregion


        #region IObserver implementation for modified document notifications

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(AvaloniaPropertyChangedEventArgs value)
        {

        }

        #endregion


        #region Private variables

        private string FileName = "";

        /// <summary>
        /// List of sprites patterns controls in the set
        /// </summary>
        private List<SpritePatternControl> SpritePatternsList = null;

        /// <summary>
        /// last zoom value
        /// </summary>
        private int lastZoom = 0;

        /// <summary>
        /// Zooms values
        /// </summary>
        private int[] zooms = new int[]
        {
            1,2,4,8,16,24,32,48,64
        };

        private byte actualFrame = 0;

        private FoldingManager? fManager;
        private DispatcherTimer? updateFoldingsTimer;

        #endregion


        #region ZXDocumentBase functions

        public override bool SaveDocument(TextWriter OutputLog)
        {
            try
            {
                var masterList = SpritePatternsList.Select(d => d.SpriteData).ToArray();
                var sprList = new List<Sprite>();
                foreach (var spr in masterList)
                {
                    sprList.Add(spr); //.Clonar<Sprite>());
                }
                foreach (Sprite spr in sprList)
                {
                    if (spr == null)
                    {
                        continue;
                    }

                    if (spr.Frames < spr.Patterns.Count())
                    {
                        spr.Patterns = spr.Patterns.Take(spr.Frames).ToList();
                    }
                }

                var dataJSon = sprList.Serializar();
                //if (!ServiceLayer.Files_SaveFileString(FileName, dataJSon))
                //{
                //    return false;
                //}
                //;

                _Modified = false;
                DocumentSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                OutputLog.WriteLine($"Error saving file {FileName}: {ex.Message}");
                return false;
            }
        }


        public override bool RenameDocument(string NewName, TextWriter OutputLog)
        {
            FileName = NewName;
            return true;
        }


        public override bool CloseDocument(TextWriter OutputLog, bool ForceClose)
        {
            return true;
        }


        public override void Dispose()
        {
        }


        public override void Activated()
        {
            this.Focus();
            //ctrlPreview.Start();
            base.Activated();
        }


        public override void Deactivated()
        {
            //ctrlPreview.Stop();
            base.Deactivated();
        }

        #endregion





        public List<ControlItem> Controls { get; internal set; }

        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }


        /// <summary>
        /// Zoom level of the editor
        /// </summary>
        public int Zoom { get; set; }


        /// <summary>
        /// Defines is grid is visible
        /// </summary>
        public bool IsGridOn { get; set; }


        private ControlItem Viewport = null;
        //private PaletteColor[] Palette=null;


        public ZXMapsEditor(string fileName)
        {
            FileName= fileName;
            InitializeComponent();
        }


        public bool Initialize()
        {
            Controls = new List<ControlItem>();
            //Palette = log.ServiceLayer.CrateNextDefaultPalette();
            Zoom = 4;
            WindowWidth = 300;
            WindowHeight = 226;
            Refresh();

            return true;
        }




        /// <summary>
        /// Refresh the editor UI
        /// </summary>
        public void Refresh()
        {            
            //cnvEditor.Children.Clear();
            Viewport = Controls.FirstOrDefault(d => d.Id == 0);
            if (Viewport == null)
            {
                Viewport = new ControlItem()
                {
                    ContainerId = 0,
                    ControlType = ControlsTypes.Panel,
                    Height = WindowHeight,
                    Id = 0,
                    Ink = 0,
                    Left = 0,
                    Name = "Main panel",
                    Paper = 255,
                    Top = 0,
                    Visible = true,
                    Width = WindowWidth,
                };
                Viewport.Properties = new List<ControlProperty>();
                Controls.Add(Viewport);
            }            

            foreach (var control in Controls.OrderBy(d=>d.Id))
            {
                switch(control.ControlType)
                {
                    case ControlsTypes.Panel:
                        Draw_Panel(control);
                        break;
                }
            }

            //cnvEditor.Width = WindowWidth * Zoom;
            //cnvEditor.Height = WindowHeight * Zoom;
       }


        public void Draw_Panel(ControlItem control)
        {
            //var r = new Rectangle();
            //r.Width = Zoom * control.Width;
            //r.Height = Zoom *control.Height;
            //r.Stroke = Palette[control.Paper].Brush;
            //r.Fill = Palette[control.Paper].Brush;
            ////cnvEditor.Children.Add(r);
            //Canvas.SetTop(r, control.Top*Zoom);
            //Canvas.SetLeft(r, control.Left*Zoom);
        }
    }
}
