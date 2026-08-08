using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.Classes;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXMaps.Neg;

namespace ZXBasicStudio.DocumentEditors.ZXMaps.Log
{
    public static class ServiceLayer_Maps
    {

        public static ZXMapsMap Maps_LoadMapByName(string name)
        {
            try
            {
                var fileName = Path.Combine(ZXProjectManager.Current.ProjectPath, name + ".zxmap");
                if (File.Exists(fileName))
                {
                    var json = File.ReadAllText(fileName);
                    return json.Deserializar<ZXMapsMap>();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error loading file {FileName}: {ex.Message}");
                return null;
            }
        }


        public static bool Maps_SaveMap(string fileName, ZXMapsMap map)
        {
            try
            {
                //var fileName = Path.Combine(ZXProjectManager.Current.ProjectPath, map.Name + ".zxmap");
                var json = map.Serializar();
                File.WriteAllText(fileName, json);
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error saving file {FileName}: {ex.Message}");
                return false;
            }
        }


        public static ZXMapsTiles Tiles_LoadMapsTiles(string fileName)
        {
            try
            {
                var json = File.ReadAllText(fileName);
                return json.Deserializar<ZXMapsTiles>();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error saving file {FileName}: {ex.Message}");
                return null;
            }
        }
    }
}
