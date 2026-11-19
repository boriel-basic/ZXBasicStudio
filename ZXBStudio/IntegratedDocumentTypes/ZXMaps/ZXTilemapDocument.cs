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
using ZXBasicStudio.DocumentEditors.ZXMaps;
using ZXBasicStudio.DocumentModel.Enums;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.IntegratedDocumentTypes.ZXGraphics
{
    public class ZXTilemapDocument : IZXDocumentType
    {
        static readonly string[] _docExtensions = { ".zxtil" };
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
            TileEditor.keyboardCommands["Save"],
            TileEditor.keyboardCommands["Cut"],
            TileEditor.keyboardCommands["Copy"],
            TileEditor.keyboardCommands["Paste"],
            TileEditor.keyboardCommands["Clear"],
            TileEditor.keyboardCommands["Undo"],
            TileEditor.keyboardCommands["Redo"],
            TileEditor.keyboardCommands["Rotate Right"],
            TileEditor.keyboardCommands["Rotate Left"],
            TileEditor.keyboardCommands["Horizontal Mirror"],
            TileEditor.keyboardCommands["Vertical Mirror"],
            TileEditor.keyboardCommands["Shift Up"],
            TileEditor.keyboardCommands["Shift Right"],
            TileEditor.keyboardCommands["Shift Down"],
            TileEditor.keyboardCommands["Shift Left"],
            TileEditor.keyboardCommands["Move Up"],
            TileEditor.keyboardCommands["Move Right"],
            TileEditor.keyboardCommands["Move Down"],
            TileEditor.keyboardCommands["Move Left"],
            TileEditor.keyboardCommands["Invert"],
            TileEditor.keyboardCommands["Mask"],
            TileEditor.keyboardCommands["Export"],
            TileEditor.keyboardCommands["Zoom In"],
            TileEditor.keyboardCommands["Zoom Out"],
        };


        public ZXTilemapDocument()
        {
            _exportManager.Initialize(DocumentEditors.ZXGraphics.neg.FileTypes.Tile);
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
