using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    public static class statusLabel
    {
        public static int rowsCount { get; set; }
        public static int columnCount { get; set; }
        public static Stopwatch stopwatch { get; set; }
        public static int rowIndex { get; set; }
        public static int columIndex { get; set; }

        public static string mainStatusLabel()
        {
            string timeConsumed = $"读取耗时: {stopwatch.ElapsedMilliseconds} 毫秒";

            // 如果耗时较长，可以自动转换为秒（可选优化逻辑）
            if (stopwatch.Elapsed.TotalSeconds >= 1)
            {
                timeConsumed = $"读取耗时: {stopwatch.Elapsed.TotalSeconds:F2} 秒"; // 保留两位小数
            }
            return "总行数："+ rowsCount+" 总计列："+ columnCount+" | "+ timeConsumed+" | "+"当前行："+ rowIndex+"/"+ rowsCount+" | "+"当前列："+ columIndex+"/"+ columnCount;
        }
    }
    public static class titleStatusLabel
    {
        public static string fileName { get; set; }
        public static string sourceFile { get; set; }

        public static string toTitle()
        {
            if (string.IsNullOrEmpty(sourceFile) && string.IsNullOrEmpty(fileName))
            {
                return "DntEditor_Hang";
            }else if (string.IsNullOrEmpty(sourceFile))
            {

                return "DntEditor_Hang" + " - [" + fileName + "]";
            }

            return "DntEditor_Hang" + " - [" + fileName + "]     使用翻译源：["+ sourceFile+"]";

        }
    }

 }
