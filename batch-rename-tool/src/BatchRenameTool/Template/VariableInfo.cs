using System;
using System.Collections.Generic;

namespace BatchRenameTool.Template
{
    /// <summary>
    /// Variable type enumeration
    /// </summary>
    public enum VariableType
    {
        String,   // String variables: name, ext, fullname
        Number,   // Number variables: i, iv
        Date,     // Date variables: today
        DateTime, // DateTime variables: now
        Image,    // Image variables: image (width, height)
        File,     // File variables: file (size, creationTime, etc.)
        Size      // Size variables: size (file size with format)
    }

    /// <summary>
    /// Format option for variable formatting
    /// </summary>
    public class FormatOption
    {
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Variable information including name, type, description, and format options
    /// </summary>
    public class VariableInfo
    {
        public string VariableName { get; set; } = string.Empty;
        public VariableType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<FormatOption> FormatOptions { get; set; } = new List<FormatOption>();

        /// <summary>
        /// Get number format options (shared by i and iv variables)
        /// </summary>
        private static List<FormatOption> GetNumberFormatOptions()
        {
            return new List<FormatOption>
            {
                new FormatOption { Text = "零", Description = "中文序号，从零开始：零, 一, 二..." },
                new FormatOption { Text = "1", Description = "序号，从1开始：1, 2, 3..." },
                new FormatOption { Text = "一", Description = "中文序号，从一开始：一, 二, 三..." },
                new FormatOption { Text = "壹", Description = "中文序号（大写），从壹开始：壹, 贰, 叁..." },
                new FormatOption { Text = "00", Description = "2位数字，从00开始：00, 01, 02..." },
                new FormatOption { Text = "01", Description = "2位数字，从01开始：01, 02, 03..." },
                new FormatOption { Text = "000", Description = "3位数字，从000开始：000, 001, 002..." },
                new FormatOption { Text = "001", Description = "3位数字，从001开始：001, 002, 003..." },
                new FormatOption { Text = "0000", Description = "4位数字，从0000开始：0000, 0001, 0002..." },
                new FormatOption { Text = "0001", Description = "4位数字，从0001开始：0001, 0002, 0003..." },
                new FormatOption { Text = "00000", Description = "5位数字，从00000开始：00000, 00001, 00002..." },
                new FormatOption { Text = "00001", Description = "5位数字，从00001开始：00001, 00002, 00003..." }
            };
        }

        /// <summary>
        /// Get all predefined variable infos
        /// </summary>
        public static List<VariableInfo> GetAllVariables()
        {
            // Shared format options for number variables
            var numberFormatOptions = GetNumberFormatOptions();

            return new List<VariableInfo>
            {
                // String variables
                new VariableInfo
                {
                    VariableName = "name",
                    Type = VariableType.String,
                    Description = "原文件名（不含扩展名）",
                    FormatOptions = new List<FormatOption>() // String variables don't support format
                },
                new VariableInfo
                {
                    VariableName = "ext",
                    Type = VariableType.String,
                    Description = "文件扩展名（不含点号）",
                    FormatOptions = new List<FormatOption>() // String variables don't support format
                },
                new VariableInfo
                {
                    VariableName = "fullname",
                    Type = VariableType.String,
                    Description = "完整文件名（包含扩展名）",
                    FormatOptions = new List<FormatOption>() // String variables don't support format
                },

                // Number variable
                new VariableInfo
                {
                    VariableName = "i",
                    Type = VariableType.Number,
                    Description = "序号变量，从0开始。可使用格式：{i:00}, {i:01}, {i:1}, {i:零}, {i:一}, {i:壹}。支持表达式：{i:2*i+1:000}, {i:i*3-2:00}",
                    FormatOptions = numberFormatOptions
                },

                // Reverse index variable
                new VariableInfo
                {
                    VariableName = "iv",
                    Type = VariableType.Number,
                    Description = "倒序序号变量，从最后一个开始倒序。可使用格式：{iv:00}, {iv:01}, {iv:1}, {iv:零}, {iv:一}, {iv:壹}。例如：10个文件时，第0个文件的iv=9，第9个文件的iv=0",
                    FormatOptions = numberFormatOptions
                },

                // Date variable
                new VariableInfo
                {
                    VariableName = "today",
                    Type = VariableType.Date,
                    Description = "当前日期。可使用格式：{today:yyyy-MM-dd}, {today:yyyy年MM月dd日} 等",
                    FormatOptions = new List<FormatOption>
                    {
                        new FormatOption { Text = "yyyy-MM-dd", Description = "日期格式：2024-01-01" },
                        new FormatOption { Text = "yyyy年MM月dd日", Description = "日期格式：2024年01月01日" },
                        new FormatOption { Text = "yyyy/MM/dd", Description = "日期格式：2024/01/01" },
                        new FormatOption { Text = "yyyyMMdd", Description = "日期格式：20240101" },
                        new FormatOption { Text = "MM-dd", Description = "日期格式：01-01" },
                        new FormatOption { Text = "MM月dd日", Description = "日期格式：01月01日" }
                    }
                },

                // DateTime variable
                new VariableInfo
                {
                    VariableName = "now",
                    Type = VariableType.DateTime,
                    Description = "当前日期时间。可使用格式：{now:yyyyMMddHHmmss}, {now:yyyy年MM月dd日 HH时mm分} 等（注意：文件名不支持冒号）",
                    FormatOptions = new List<FormatOption>
                    {
                        new FormatOption { Text = "yyyy年MM月dd日 HH时mm分ss秒", Description = "日期时间格式：2024年01月01日 12时30分45秒" },
                        new FormatOption { Text = "yyyy年MM月dd日 HH时mm分", Description = "日期时间格式：2024年01月01日 12时30分" },
                        new FormatOption { Text = "yyyyMMddHHmmss", Description = "日期时间格式：20240101123045" },
                        new FormatOption { Text = "yyyyMMddHHmm", Description = "日期时间格式：202401011230" },
                        new FormatOption { Text = "HH时mm分ss秒", Description = "时间格式：12时30分45秒" },
                        new FormatOption { Text = "HH时mm分", Description = "时间格式：12时30分" },
                        new FormatOption { Text = "yyyy-MM-dd HHmmss", Description = "日期时间格式：2024-01-01 123045" },
                        new FormatOption { Text = "yyyy-MM-dd HHmm", Description = "日期时间格式：2024-01-01 1230" }
                    }
                },

                // Image variable
                new VariableInfo
                {
                    VariableName = "image",
                    Type = VariableType.Image,
                    Description = "图像分辨率变量。可使用格式：{image:w} 宽度，{image:h} 高度，{image:wxh} 宽度x高度",
                    FormatOptions = new List<FormatOption>
                    {
                        new FormatOption { Text = "w", Description = "图像宽度（像素）" },
                        new FormatOption { Text = "h", Description = "图像高度（像素）" },
                        new FormatOption { Text = "wxh", Description = "宽度x高度（例如：1920x1080）" }
                    }
                },

                // File variable (placeholder for future expansion with file.createTime, etc.)
                new VariableInfo
                {
                    VariableName = "file",
                    Type = VariableType.File,
                    Description = "文件信息变量。当前支持：{file} 返回文件路径（后续将支持 file.createTime, file.editTime 等）",
                    FormatOptions = new List<FormatOption>() // Will be expanded later
                },

                // Size variable
                new VariableInfo
                {
                    VariableName = "size",
                    Type = VariableType.Size,
                    Description = "文件大小变量。可使用格式：{size:1b} 以字节显示，{size:1kb} 以KB显示，{size:1mb} 以MB显示，{size:.2f} 自动单位（保留2位小数）",
                    FormatOptions = new List<FormatOption>
                    {
                        new FormatOption { Text = "1b", Description = "以字节为单位显示（例如：1024 B）" },
                        new FormatOption { Text = "1kb", Description = "以KB为单位显示（例如：1024 KB）" },
                        new FormatOption { Text = "1mb", Description = "以MB为单位显示（例如：1.5 MB）" },
                        new FormatOption { Text = ".2f", Description = "自动单位，保留2位小数（例如：1.50 MB）" },
                        new FormatOption { Text = ".1f", Description = "自动单位，保留1位小数（例如：1.5 MB）" },
                        new FormatOption { Text = ".0f", Description = "自动单位，整数显示（例如：2 MB）" }
                    }
                }
            };
        }

        /// <summary>
        /// Get variable info by name (case-insensitive)
        /// </summary>
        public static VariableInfo? GetVariable(string variableName)
        {
            var allVars = GetAllVariables();
            return allVars.Find(v => v.VariableName.Equals(variableName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
