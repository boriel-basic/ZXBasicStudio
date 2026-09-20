using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXGraphics;
using ZXBasicStudio.DocumentEditors.ZXMaps;
using ZXBasicStudio.DocumentEditors.ZXTextEditor.Controls;
using ZXBasicStudio.DocumentModel.Classes;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.IntegratedDocumentTypes.ZXGraphics
{
    public class ZXTilemapDocumentFactory : IZXDocumentFactory
    {
        public bool CreateDocument(string Path, TextWriter OutputLog)
        {
            var type = ZXDocumentProvider.GetDocumentType(Path);

            if (type is not ZXTilemapDocument)
            {
                OutputLog.WriteLine($"Document {Path} is not a tilemap file, internal document handling error, operation aborted.");
                return false;
            }

            if (File.Exists(Path))
            {
                OutputLog.WriteLine($"File {Path} already exists, cannot create.");
                return false;
            }

            try
            {
                File.Create(Path).Dispose();
                return true;
            }
            catch (Exception ex)
            {
                OutputLog.WriteLine($"Error creating file {Path}: {ex.Message}");
                return false;
            }
        }

        public ZXDocumentEditorBase? CreateEditor(string Path, TextWriter OutputLog)
        {
            var type = ZXDocumentProvider.GetDocumentType(Path);

            if (type is not ZXTilemapDocument)
            {
                OutputLog.WriteLine($"Document {Path} is not a tile file, internal document handling error, operation aborted.");
                return null;
            }
            TileEditor editor = new TileEditor(Path);
            return editor;
        }
    }
}
