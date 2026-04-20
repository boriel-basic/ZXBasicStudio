using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.Classes;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentModel.Enums;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.IntegratedDocumentTypes.ZXGraphics
{
    public class ZXTilemapDocument : IZXDocumentType
    {
        static readonly string[] _docExtensions = { ".til", ".zxtil" };
        static readonly string _docName = "Tilemap file";
        static readonly string _docDesc = "Tilemap files allow you to create and modify a set of tiles for use in maps. Once created, it can be used in the zxmap built-in editor.";
        static readonly string _docCat = "Graphics";
        static readonly string _docAspect = "/Svg/Documents/file-tile.svg";
        static readonly Guid _docId = Guid.Parse("baed2950-b902-44dd-aac6-4912b533684f");
        public static Guid Id => _docId;

        static readonly ZXTilemapDocumentFactory _factory = new ZXTilemapDocumentFactory();
        Bitmap? _icon;

        public string[] DocumentExtensions => _docExtensions;
        public string DocumentName => _docName;
        public string DocumentDescription => _docDesc;
        public string DocumentCategory => _docCat;
        public string? DocumentAspect => _docAspect;

        private static readonly ExportManager _exportManager = new ExportManager();

        static readonly ZXKeybCommand[] _editCommands = new ZXKeybCommand[]
        {
            SpriteEditor.keyboardCommands["Save"],
            SpriteEditor.keyboardCommands["Cut"],
            SpriteEditor.keyboardCommands["Copy"],
            SpriteEditor.keyboardCommands["Paste"],
            SpriteEditor.keyboardCommands["Clear"],
            SpriteEditor.keyboardCommands["Undo"],
            SpriteEditor.keyboardCommands["Redo"],
            SpriteEditor.keyboardCommands["Rotate Right"],
            SpriteEditor.keyboardCommands["Rotate Left"],
            SpriteEditor.keyboardCommands["Horizontal Mirror"],
            SpriteEditor.keyboardCommands["Vertical Mirror"],
            SpriteEditor.keyboardCommands["Shift Up"],
            SpriteEditor.keyboardCommands["Shift Right"],
            SpriteEditor.keyboardCommands["Shift Down"],
            SpriteEditor.keyboardCommands["Shift Left"],
            SpriteEditor.keyboardCommands["Move Up"],
            SpriteEditor.keyboardCommands["Move Right"],
            SpriteEditor.keyboardCommands["Move Down"],
            SpriteEditor.keyboardCommands["Move Left"],
            SpriteEditor.keyboardCommands["Invert"],
            SpriteEditor.keyboardCommands["Mask"],
            SpriteEditor.keyboardCommands["Export"],
            SpriteEditor.keyboardCommands["Zoom In"],
            SpriteEditor.keyboardCommands["Zoom Out"],
        };


        public ZXTilemapDocument()
        {
            _exportManager.Initialize(DocumentEditors.ZXGraphics.neg.FileTypes.Sprite);
        }


        public Bitmap DocumentIcon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = new Bitmap(AssetLoader.Open(new Uri("avares://ZXBasicStudio/Assets/zxmaps_til.png")));
                }

                return _icon;
            }
        }

        public bool CanCreate => true;

        public bool CanEdit => true;

        public IZXDocumentFactory DocumentFactory => _factory;

        public IZXDocumentBuilder? DocumentBuilder => _exportManager;

        public ZXBuildStage? DocumentBuildStage => ZXBuildStage.PreBuild;

        Guid IZXDocumentType.DocumentTypeId => _docId;

        ZXKeybCommand[]? IZXDocumentType.EditorCommands => _editCommands;
    }
}
