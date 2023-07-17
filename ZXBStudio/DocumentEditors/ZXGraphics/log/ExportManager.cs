﻿using CoreSpectrum.SupportClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXBasicStudio.BuildSystem;
using ZXBasicStudio.Classes;
using ZXBasicStudio.Common;
using ZXBasicStudio.DocumentEditors.ZXGraphics.neg;
using ZXBasicStudio.DocumentModel.Enums;
using ZXBasicStudio.DocumentModel.Interfaces;

namespace ZXBasicStudio.DocumentEditors.ZXGraphics.log
{
    /// <summary>
    /// Class to export on prebuild operation
    /// </summary>
    public class ExportManager : IZXDocumentBuilder
    {
        private FileTypes fileType = FileTypes.Undefined;

        public bool Initialize(FileTypes fileType)
        {
            this.fileType = fileType;
            return true;
        }


        public bool Build(string BuildPath, ZXBuildType BuildType, ZXProgram? program, TextWriter OutputLog)
        {
            if (!ServiceLayer.Initialized)
            {
                ServiceLayer.Initialize();
            }

            // Get all config filenames
            var files = ServiceLayer.Files_GetAllConfigFiles(BuildPath, fileType);
            if (files == null)
            {
                return true;
            }

            foreach (var file in files)
            {
                // Get export config data from config file
                var exportConfig = ServiceLayer.Export_GetConfigFile(file);
                if (exportConfig == null)
                {
                    continue;
                }

                // Build configuration sources
                var fileNameData = file.Replace(".zbs", "");
                var fileTypeConfig = ServiceLayer.GetFileType(fileNameData);
                var fileData = ServiceLayer.GetFileData(fileNameData);
                if (fileData == null || fileData.Length == 0)
                {
                    continue;
                }
                var patterns = CreatePatterns(fileTypeConfig, fileData);

                Export(exportConfig, fileTypeConfig, patterns);
            }
            return true;
        }


        /// <summary>
        /// Export one file
        /// </summary>
        /// <param name="exportConfig">ExportConfig of the file</param>
        /// <returns></returns>
        public bool Export(ExportConfig exportConfig, FileTypeConfig fileTypeConfig, Pattern[] patterns)
        {
            // Export depending on the type
            string exportedData = "";
            switch (exportConfig.ExportType)
            {
                case ExportTypes.Bin:
                    Export_BIN(fileTypeConfig, patterns, exportConfig.ExportFilePath);
                    return true;
                case ExportTypes.Tap:
                    Export_TAP(fileTypeConfig, patterns, exportConfig);
                    break;
                case ExportTypes.Asm:
                    exportedData = Export_ASM(fileTypeConfig, patterns, exportConfig.LabelName);
                    break;
                case ExportTypes.Dim:
                    exportedData = Export_DIM(fileTypeConfig, patterns, exportConfig.LabelName, exportConfig.ArrayBase);
                    break;
                case ExportTypes.Data:
                    exportedData = Export_DATA(fileTypeConfig, patterns, exportConfig.LabelName);
                    break;
                default:
                    return true;
            }

            if (!string.IsNullOrEmpty(exportedData))
            {
                var sb = new StringBuilder();
                sb.AppendLine("'------------------------------------------------------------------------------");
                sb.AppendLine("'- Build data generated by ZXBasicStudio -------------------------------------");
                sb.AppendLine("'- Do not modify this file, its contents are deleted at each build -----------");
                sb.AppendLine("'------------------------------------------------------------------------------");
                sb.AppendLine("");
                sb.Append(exportedData);
                ServiceLayer.Files_SaveFileString(exportConfig.ExportFilePath, sb.ToString());
            }
            return true;
        }


        /// <summary>
        /// Create patterns from binary file data
        /// </summary>
        /// <param name="fileType">FileTypeConfig</param>
        /// <param name="fileData">Binary data</param>
        /// <returns>Array of Patterns or null if error</returns>
        public Pattern[] CreatePatterns(FileTypeConfig fileType, byte[] fileData)
        {
            var patterns = new List<Pattern>();

            for (int n = 0; n < fileType.NumerOfPatterns; n++)
            {
                var p = new Pattern();
                p.Id = n;
                p.Number = "";
                switch (fileType.FileType)
                {
                    case FileTypes.UDG:
                        {
                            var id = n;
                            p.Number = id.ToString();
                            var c = Convert.ToChar(n + 65);
                            p.Name = c.ToString();
                        }
                        break;
                    case FileTypes.Font:
                        {
                            var id = n + 32;
                            p.Number = id.ToString();
                            var c = Convert.ToChar(n + 32);
                            p.Name = c.ToString();
                        }
                        break;
                    default:
                        p.Number = n.ToString();
                        p.Name = "";
                        break;
                }
                p.Data = ServiceLayer.Binary2PointData(n, fileData, 0, 0);
                patterns.Add(p);
            }
            return patterns.ToArray();
        }


        /// <summary>
        /// Export to ASM format
        /// </summary>
        /// <param name="fileType">FileTyeConfig</param>
        /// <param name="patterns">Patterns with the source data</param>
        /// <param name="labelName">Name of the label</param>
        /// <returns>String with de exported data</returns>
        public static string Export_ASM(FileTypeConfig fileType, Pattern[] patterns, string labelName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(labelName + ":");
            sb.AppendLine("ASM");

            var data = ServiceLayer.Files_CreateBinData_GDUorFont(fileType, patterns);
            int col = 0;

            foreach (var d in data)
            {
                if (col == 0)
                {
                    sb.Append("\tDB ");
                }
                if (col > 0)
                {
                    sb.Append(",");
                }
                var x = string.Format("${0:X2}", d);
                sb.Append(x);

                col++;
                if (col >= 8)
                {
                    sb.AppendLine("");
                    col = 0;
                }
            }

            sb.AppendLine("END ASM");

            return sb.ToString();
        }


        /// <summary>
        /// Export to DATA format
        /// </summary>
        /// <param name="fileType">FileTyeConfig</param>
        /// <param name="patterns">Patterns with the source data</param>
        /// <param name="labelName">Name of the label</param>
        /// <returns>String with de exported data</returns>
        public static string Export_DATA(FileTypeConfig fileType, Pattern[] patterns, string labelName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(labelName + ":");

            var data = ServiceLayer.Files_CreateBinData_GDUorFont(fileType, patterns);
            int col = 0;
            int row = 0;
            foreach (var d in data)
            {
                if (col == 0)
                {
                    sb.Append("DATA ");
                }
                if (col > 0)
                {
                    sb.Append(",");
                }
                var x = string.Format("${0:X2}", d);
                sb.Append(x);

                col++;
                if (col >= 8)
                {
                    sb.AppendLine("");
                    col = 0;
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Export to DIM format
        /// </summary>
        /// <param name="fileType">FileTyeConfig</param>
        /// <param name="patterns">Patterns with the source data</param>
        /// <param name="labelName">Name of the label</param>
        /// <param name="arrayBase">Array base for DIM</param>
        /// <returns>String with de exported data</returns>
        public static string Export_DIM(FileTypeConfig fileType, Pattern[] patterns, string labelName, int arrayBase)
        {
            int la = 20;
            int bt = 7;

            switch (fileType.FileType)
            {
                case FileTypes.UDG:
                    la = 20;
                    break;
                case FileTypes.Font:
                    la = 95;
                    break;
            }

            switch (arrayBase)
            {
                case 1:
                    la++;
                    bt = 8;
                    break;
                case 2:
                    {
                        var settings = ServiceLayer.GetProjectSettings();
                        if (settings != null)
                        {
                            if (settings.ArrayBase == 1)
                            {
                                la++;
                                bt = 8;
                            }
                        }

                    }
                    break;
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("DIM {0}({1},{2}) AS UBYTE => {{ _", labelName, la, bt));

            var data = ServiceLayer.Files_CreateBinData_GDUorFont(fileType, patterns);
            int col = 0;
            int row = 0;
            foreach (var d in data)
            {
                if (col == 0)
                {
                    if (row == 0)
                    {
                        row = 1;
                    }
                    else
                    {
                        sb.AppendLine(", _");
                    }
                    sb.Append("\t{ ");
                }
                if (col > 0)
                {
                    sb.Append(",");
                }
                var x = string.Format("${0:X2}", d);
                sb.Append(x);

                col++;
                if (col >= 8)
                {
                    sb.Append(" }");
                    col = 0;
                }
            }
            sb.AppendLine(" _");
            sb.AppendLine("}");

            return sb.ToString();
        }


        /// <summary>
        /// Export to BIN (RAW) format
        /// </summary>
        /// <param name="fileType">FileTyeConfig</param>
        /// <param name="patterns">Patterns with the source data</param>
        /// <param name="fileName">Output file name</param>
        public static void Export_BIN(FileTypeConfig fileType, Pattern[] patterns, string fileName)
        {
            var ft = fileType.Clonar<FileTypeConfig>();
            ft.FileName = fileName;
            ServiceLayer.Files_Save_GDUorFont(fileType, patterns);
        }


        /// <summary>
        /// Export to TAP format
        /// </summary>
        /// <param name="fileType">FileTyeConfig</param>
        /// <param name="patterns">Patterns with the source data</param>
        /// <param name="fileName">Output file name</param>
        public static void Export_TAP(FileTypeConfig fileType, Pattern[] patterns, ExportConfig exportConfig)
        {
            var data = ServiceLayer.Files_CreateBinData_GDUorFont(fileType, patterns);
            data = ServiceLayer.Bin2Tap(exportConfig.ZXFileName, exportConfig.ZXAddress, data);
            ServiceLayer.Files_SaveFileData(exportConfig.ExportFilePath, data);
        }
    }
}
