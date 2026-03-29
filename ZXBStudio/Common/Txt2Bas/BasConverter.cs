using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZXBasicStudio.Common.Txt2Bas
{
    /// <summary>
    /// Converts plain text into tokenized Spectrum BASIC binary format.
    /// Based on Yoruguaman work: https://github.com/Ultrahead/SpeccyNextTools
    /// </summary>
    internal class BasConverter
    {
        private readonly TokenMap _tokenMap;
        private readonly List<string> _sortedKeys;

        /// <summary>
        /// The Auto-start line number. 
        /// Defaults to 32768 (No Auto-start). 
        /// Set only via the #autostart directive in the source file.
        /// </summary>
        public int AutoStartLine { get; private set; } = 32768;

        /// <summary>
        /// Initializes member variable fields of the <see cref="BasConverter"/> class.
        /// </summary>
        public BasConverter()
        {
            // 1. Initialize the TokenMap dictionary.
            // 2. Create a list of keys sorted by Length Descending.
            //    (This ensures greedy matching: e.g., "DEFPROC" is matched before "DEF").

            _tokenMap = new TokenMap();
            _sortedKeys = _tokenMap.Map.Keys
                .OrderByDescending(k => k.Length)
                .ToList();
        }

        /// <summary>
        /// Reads a text file and converts it to a byte array of tokenized BASIC.
        /// </summary>
        /// <param name="textData">Text to convert</param>
        /// <returns>Byte array representing the BASIC program.</returns>
        public byte[] ConvertFile(string textData)
        {
            // 1. Read all lines from the source text file.
            // 2. Initialize state variables (auto-line counter, output buffer).
            // 3. Iterate through each line:
            //    a. Skip whitespace-only lines (source code formatting).
            //    b. Process directive (#autostart) then skip the line.
            //    c. Skip all other lines starting with # (source code comments).
            //    d. Parse explicit or implicit line numbers and tokenize content.
            // 4. Return the aggregated binary data.

            string[] lines = textData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var output = new List<byte>();

            int currentLineNum = 10;

            foreach (string line in lines)
            {
                string text = line.Trim();

                // 1. Skip Empty Lines completely (do not generate a BASIC line)
                if (string.IsNullOrWhiteSpace(text)) continue;

                // 2. Handle Lines starting with #
                if (text.StartsWith("#"))
                {
                    // Check for directives
                    if (text.StartsWith("#autostart", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1 && int.TryParse(parts[1], out int autoStartVal))
                        {
                            AutoStartLine = autoStartVal;
                        }
                    }

                    // Whether it was a directive or a comment, skip it in the output.
                    continue;
                }

                // 3. Handle Standard Lines
                int lineNum = currentLineNum;
                string restOfLine = text;

                Match match = Regex.Match(text, @"^(\d+)\s+(.*)");

                if (match.Success)
                {
                    lineNum = int.Parse(match.Groups[1].Value);
                    restOfLine = match.Groups[2].Value;
                    currentLineNum = lineNum + 10;
                }
                else
                {
                    currentLineNum += 10;
                }

                byte[] lineBytes = ParseLine(lineNum, restOfLine);
                output.AddRange(lineBytes);
            }

            return output.ToArray();
        }

        /// <summary>
        /// Parses a single line of text into binary line format.
        /// Structure: [LineNum(BE)] [Length(LE)] [Data...] [0x0D]
        /// </summary>
        /// <param name="lineNum">The line number.</param>
        /// <param name="text">The text content of the line.</param>
        /// <returns>A byte array representing the binary line.</returns>
        private byte[] ParseLine(int lineNum, string text)
        {
            // 1. Iterate through the text character by character.
            // 2. Detect and process String Literals (preserve exactly).
            // 3. Detect and process Numbers (convert to ASCII + 5-byte hidden Sinclair format).
            // 4. Detect and process SPECIAL COMMENT (';' after colon or at start).
            // 5. Detect and process Keywords (Greedy Match against TokenMap):
            //    a. If REM found, consume the rest of the line as a comment.
            //    b. If other keyword found, strip immediately following whitespace.
            // 6. Fallback: Add character as literal ASCII.
            // 7. Append End-of-Line marker (0x0D).
            // 8. Prepend the Line Header (Line Number + Length) and return the byte array.

            List<byte> lineData = new List<byte>();

            for (int i = 0; i < text.Length; i++)
            {
                // String Literals
                if (text[i] == '"')
                {
                    int endQuote = text.IndexOf('"', i + 1);
                    if (endQuote == -1) endQuote = text.Length;

                    string literal = text.Substring(i, endQuote - i + 1);
                    lineData.AddRange(Encoding.ASCII.GetBytes(literal));
                    i = endQuote;
                    continue;
                }

                // Numbers
                if (char.IsDigit(text[i]) || (text[i] == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
                {
                    string numStr = "";
                    int j = i;
                    while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.'))
                    {
                        numStr += text[j];
                        j++;
                    }

                    if (double.TryParse(numStr, out double val))
                    {
                        lineData.AddRange(Encoding.ASCII.GetBytes(numStr));
                        lineData.Add(0x0E); // Hidden Marker
                        lineData.AddRange(SinclairNumber.Pack(val));
                        i = j - 1;
                        continue;
                    }
                }

                // COMMENT HANDLING: Strict check for ';comment' idiom
                // Trigger: Semicolon at start of line OR Semicolon immediately preceded by Colon
                if (text[i] == ';')
                {
                    bool isComment = false;

                    // Look backwards skipping whitespace to find the context
                    int back = i - 1;
                    while (back >= 0 && text[back] == ' ') back--;

                    if (back < 0) isComment = true; // Start of line
                    else if (text[back] == ':') isComment = true; // Preceded by colon

                    if (isComment)
                    {
                        // Consume the rest of the line as literal text (do not tokenize)
                        string comment = text.Substring(i);
                        lineData.AddRange(Encoding.ASCII.GetBytes(comment));
                        i = text.Length;
                        continue;
                    }
                }

                // Keywords
                bool matched = false;
                foreach (string k in _sortedKeys)
                {
                    if (i + k.Length > text.Length) continue;

                    if (string.Compare(text.Substring(i, k.Length), k, StringComparison.OrdinalIgnoreCase) != 0)
                        continue;

                    bool isAlphaToken = char.IsLetter(k[0]);
                    bool prevCharValid = (i == 0) || !char.IsLetter(text[i - 1]);
                    bool nextCharValid = (i + k.Length >= text.Length) || !char.IsLetterOrDigit(text[i + k.Length]);

                    if (isAlphaToken && (!prevCharValid || !nextCharValid)) continue;

                    byte token = _tokenMap.Map[k];
                    lineData.Add(token);
                    i += k.Length;
                    matched = true;

                    // REM handling
                    if (token == 0xEA)
                    {
                        if (i < text.Length)
                        {
                            string comment = text.Substring(i);
                            lineData.AddRange(Encoding.ASCII.GetBytes(comment));
                            i = text.Length;
                        }
                    }
                    else
                    {
                        // Strip trailing space
                        while (i < text.Length && text[i] == ' ') i++;
                    }

                    i--;
                    break;
                }

                if (matched) continue;

                // Literal
                lineData.Add((byte)text[i]);
            }

            lineData.Add(0x0D);

            // Construct Line Header
            List<byte> finalLine =
            [
                (byte)((lineNum >> 8) & 0xFF),
                (byte)(lineNum & 0xFF)
            ];

            int length = lineData.Count;
            finalLine.Add((byte)(length & 0xFF));
            finalLine.Add((byte)((length >> 8) & 0xFF));

            finalLine.AddRange(lineData);

            return finalLine.ToArray();
        }
    }
}
