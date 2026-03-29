using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZXBasicStudio.Common.Txt2Bas
{
    /// <summary>
    /// Converts plain text to ZX Spectrum .bas format
    /// Based on Yoruguaman work: https://github.com/Ultrahead/SpeccyNextTools
    /// </summary>
    public static class Txt2BasConverter
    {

        public static byte[] Text2Bas(string text)
        {
            try
            {
                BasConverter converter = new BasConverter();
                byte[] basData = converter.ConvertFile(text);

                // Use the explicitly parsed AutoStartLine (defaults to 32768 if no #autostart found)
                byte[] header = Plus3Dos.CreateHeader(basData.Length, converter.AutoStartLine);

                var fileData = new List<byte>();
                fileData.AddRange(header);
                fileData.AddRange(basData);

                return fileData.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}
