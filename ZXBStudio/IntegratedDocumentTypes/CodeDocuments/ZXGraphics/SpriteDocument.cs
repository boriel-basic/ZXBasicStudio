﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXGraphics.log;
using ZXBasicStudio.DocumentModel.Enums;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.IntegratedDocumentTypes.CodeDocuments.ZXGraphics
{
    public class SpriteDocument : IZXDocumentType
    {
        static readonly string[] _docExtensions = { ".spr" };
        static readonly string _docName = "Sprites file";
        static readonly string _docDesc = "Sprite files allow you to create and modify graphics of different sizes and formats. Once created, they can be exported in multiple formats.";
        static readonly string _docCat = "Graphics";
        static readonly string _docAspect = "/Svg/Documents/file-sprite.svg";

        static readonly SpriteDocumentFactory _factory = new SpriteDocumentFactory();
        Bitmap? _icon;

        public string[] DocumentExtensions => _docExtensions;
        public string DocumentName => _docName;
        public string DocumentDescription => _docDesc;
        public string DocumentCategory => _docCat;
        public string? DocumentAspect => _docAspect;

        private static readonly ExportManager _exportManager = new ExportManager();


        public SpriteDocument()
        {
            _exportManager.Initialize(DocumentEditors.ZXGraphics.neg.FileTypes.Sprite);
        }


        public Bitmap DocumentIcon
        {
            get
            {
                if (_icon == null)
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

                    if (assets == null)
                        throw new AvaloniaInternalException("Cannot create assets loader");

                    _icon = new Bitmap(assets.Open(new Uri("avares://ZXBasicStudio/Assets/zxGraphics_spr.png")));
                }

                return _icon;
            }
        }

        public bool CanCreate => true;

        public bool CanEdit => true;

        public IZXDocumentFactory DocumentFactory => _factory;

        public IZXDocumentBuilder? DocumentBuilder => _exportManager;

        public ZXBuildStage? DocumentBuildStage => ZXBuildStage.PreBuild;
    }
}
