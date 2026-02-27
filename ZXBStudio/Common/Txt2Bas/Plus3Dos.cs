using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.Common.Txt2Bas
{
    /// <summary>
    /// Generates the 128-byte +3DOS Header required for Spectrum Next/Plus3 files.
    /// Based on Yoruguaman work: https://github.com/Ultrahead/SpeccyNextTools
    /// </summary>
    internal static class Plus3Dos
    {
        /// <summary>
        /// Creates a valid +3DOS header.
        /// </summary>
        /// <param name="basicLength">The length of the BASIC program data (excluding header).</param>
        /// <param name="autoStartLine">The line number to auto-run (or 32768 for none).</param>
        /// <returns>A 128-byte byte array containing the header.</returns>
        public static byte[] CreateHeader(int basicLength, int autoStartLine)
        {
            // 1. Allocate a zero-filled 128-byte buffer.
            // 2. Write the "PLUS3DOS" signature and Soft EOF (0x1A) to the start.
            // 3. Set the Issue (1) and Version (0) bytes.
            // 4. Calculate and write Total File Size (Header + BASIC Data) at offset 11.
            // 5. Write BASIC-specific metadata (Type, Length, Vars Offset).
            // 6. Set the Auto-start line (or 32768 if disabled).
            // 7. Calculate the Checksum (Sum of bytes 0-126 modulo 256) and write it at byte 127.

            byte[] header = new byte[128];
            Array.Clear(header, 0, 128);

            // Signature
            byte[] sig = Encoding.ASCII.GetBytes("PLUS3DOS");
            Array.Copy(sig, header, sig.Length);
            header[8] = 0x1A;

            // Version info
            header[9] = 0x01;
            header[10] = 0x00;

            // Total File Size (Header + Data)
            int totalFileSize = basicLength + 128;
            BitConverter.GetBytes(totalFileSize).CopyTo(header, 11);

            // BASIC Header Info
            header[15] = 0x00; // Type: Program
            BitConverter.GetBytes((ushort)basicLength).CopyTo(header, 16); // Length

            // Auto-start
            if (autoStartLine is >= 0 and < 32768)
            {
                BitConverter.GetBytes((ushort)autoStartLine).CopyTo(header, 18);
            }
            else
            {
                BitConverter.GetBytes((ushort)32768).CopyTo(header, 18);
            }

            // Vars Offset
            BitConverter.GetBytes((ushort)basicLength).CopyTo(header, 20);

            // Checksum
            int sum = 0;
            for (int i = 0; i < 127; i++) sum += header[i];
            header[127] = (byte)(sum % 256);

            return header;
        }
    }
}
