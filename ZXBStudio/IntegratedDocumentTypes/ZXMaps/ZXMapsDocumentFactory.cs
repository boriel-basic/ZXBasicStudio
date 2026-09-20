using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.DocumentEditors.ZXMaps;
using ZXBasicStudio.DocumentModel.Classes;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.IntegratedDocumentTypes.ZXMaps
{
    public class ZXMapsDocumentFactory : IZXDocumentFactory
    {
        public bool CreateDocument(string Path, TextWriter OutputLog)
        {
            var type = ZXDocumentProvider.GetDocumentType(Path);

            if (type is not ZXMapsDocument)
            {
                OutputLog.WriteLine($"Document {Path} is not an ZXMaps file, internal document handling error, operation aborted.");
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

            if (type is not ZXMapsDocument)
            {
                OutputLog.WriteLine($"Document {Path} is not an ZXMaps file, internal document handling error, operation aborted.");
                return null;
            }
            ZXMapsEditor editor = new ZXMapsEditor(Path);
            return editor;
        }
    }
}
