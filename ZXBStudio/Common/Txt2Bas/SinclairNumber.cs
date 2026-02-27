using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.Common.Txt2Bas
{
    /// <summary>
    /// Handles the 5-byte floating point format used by Sinclair BASIC.
    /// Based on Yoruguaman work: https://github.com/Ultrahead/SpeccyNextTools
    /// </summary>
    internal static class SinclairNumber
    {
        /// <summary>
        /// Packs a double into the 5-byte internal format.
        /// Currently, supports integer format optimization (00 Sign LSB MSB 00).
        /// </summary>
        /// <param name="number">The number to pack.</param>
        /// <returns>A 5-byte array representing the Sinclair number format.</returns>
        public static byte[] Pack(double number)
        {
            // 1. Check if the number is a small integer (within +/- 65535).
            // 2. If yes, create the integer format: 0x00, SignByte, LSB, MSB, 0x00.
            // 3. If no, return empty zero bytes (full FP implementation omitted for brevity).

            if (number % 1 == 0 && number is >= -65535 and <= 65535)
            {
                int val = (int)number;
                byte sign = 0;

                if (val < 0)
                {
                    sign = 0xFF;
                    val = -val;
                }

                return [0x00, sign, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF), 0x00];
            }

            return "\0\0\0\0\0"u8.ToArray();
        }
    }
}
