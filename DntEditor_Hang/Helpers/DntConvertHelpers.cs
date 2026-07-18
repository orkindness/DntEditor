using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;
using DntEditor_Hang.Models;

namespace DntEditor_Hang.Helpers
{
    public static class DntConvertHelpers
    {
        // 根据游戏历史特征，DNT内的英文/中文多采用 GB2312/GBK 编码存储
        private static readonly Encoding GameEncoding = Encoding.GetEncoding("GB2312");

        /// <summary>
        /// 从指定路径读取二进制DNT文件
        /// </summary>
        public static DntDocument LoadFromFile(string filePath)
        {
            // 注册编码提供程序（解决现代.NET无法识别旧版GB2312的问题）
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DntDocument doc = new DntDocument();

            // 1. 一次性将文件全部读取到内存字节数组中
            // 这样做可以确保文件被读取后，流能在瞬间被彻底 Dispose/Close
            byte[] fileData = File.ReadAllBytes(filePath);

            // 2. 利用 MemoryStream 在内存中进行读取解析，完全脱离物理文件
            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader br = new BinaryReader(ms, GameEncoding))
            {
                // 1. 读取头部 (10字节)
                doc.NullBytes = br.ReadUInt32();
                doc.FieldCount = br.ReadUInt16();
                doc.RecordCount = br.ReadUInt32();

                // 2. 读取字段描述块
                for (int i = 0; i < doc.FieldCount; i++)
                {
                    ushort textLen = br.ReadUInt16();
                    byte[] nameBytes = br.ReadBytes(textLen);
                    string fieldName = GameEncoding.GetString(nameBytes);
                    byte fieldType = br.ReadByte();

                    doc.Fields.Add(new DntFieldDescription
                    {
                        FieldName = fieldName,
                        FieldType = (DntFieldType)fieldType
                    });
                }
                // 在 DntConvertEngine 类中的 LoadFromFile 方法对应修改数据块读取部分：
                doc.InitializeColumnStorage();

                // 拿到已经注册好的 PKID 强类型列空间
                var pkidList = doc.Columns["PKID"] as List<uint>;

                for (int i = 0; i < doc.RecordCount; i++)
                {
                    // 读取 PKID 并直接分流塞入分布式字典的 "PKID" 列中
                    uint pkid = br.ReadUInt32();
                    pkidList.Add(pkid);

                    // 遍历读取并分流后面的常规数据列
                    foreach (var field in doc.Fields)
                    {
                        var columnList = doc.Columns[field.FieldName];

                        switch (field.FieldType)
                        {
                            case DntFieldType.Text:
                                ushort strLen = br.ReadUInt16();
                                byte[] strBytes = br.ReadBytes(strLen);
                                columnList.Add(GameEncoding.GetString(strBytes));
                                break;
                            case DntFieldType.BooleanInt:
                                columnList.Add(br.ReadInt32());
                                break;
                            case DntFieldType.Int32:
                                columnList.Add(br.ReadInt32());
                                break;
                            case DntFieldType.Percentage:
                                columnList.Add(br.ReadSingle() / 100f);
                                break;
                            case DntFieldType.Float:
                                columnList.Add(br.ReadSingle());
                                break;
                        }
                    }
                }
            }
            return doc;
        }
        /// <summary>
        /// 将内存中的 DntDocument 写入并保存为二进制 DNT 文件
        /// </summary>
        /// <param name="doc">需要保存的内存文档对象</param>
        /// <param name="filePath">目标保存路径</param>
        public static void SaveToFile(DntDocument doc, string filePath,bool isCyclic=false)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // 确保目标文件夹存在
            string folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            // 在 DntConvertEngine 类中的 SaveToFile 方法对应修改：
            doc.FieldCount = (ushort)doc.Fields.Count;

            // 【核心更改】总行数直接读取 Columns["PKID"] 的元素总数
            var pkidList = doc.Columns["PKID"] as List<uint>;
            doc.RecordCount = pkidList != null ? (uint)pkidList.Count : 0;
            // 【核心更改 1】改用 MemoryStream 在内存中组装原始明文数据，避免频繁计算文件物理偏移
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, GameEncoding, true)) // 注意：此处加 true 保持 ms 在 bw 销毁时不关闭
                {
                    // 1. 写入头部
                    bw.Write(doc.NullBytes);
                    bw.Write(doc.FieldCount);
                    bw.Write(doc.RecordCount);

                    // 2. 写入字段描述块
                    foreach (var field in doc.Fields)
                    {
                        byte[] nameBytes = GameEncoding.GetBytes(field.FieldName);
                        bw.Write((ushort)nameBytes.Length);
                        bw.Write(nameBytes);
                        bw.Write((byte)field.FieldType);
                    }

                    // 3. 横向组装并写入数据块
                    for (int i = 0; i < doc.RecordCount; i++)
                    {
                        bw.Write(pkidList[i]);

                        foreach (var field in doc.Fields)
                        {
                            var columnList = doc.Columns[field.FieldName];
                            object val = columnList[i];

                            switch (field.FieldType)
                            {
                                case DntFieldType.Text:
                                    byte[] strBytes = GameEncoding.GetBytes(val?.ToString() ?? string.Empty);
                                    bw.Write((ushort)strBytes.Length);
                                    bw.Write(strBytes);
                                    break;
                                case DntFieldType.BooleanInt:
                                    bw.Write((int)val);
                                    break;
                                case DntFieldType.Int32:
                                    bw.Write((int)val);
                                    break;
                                case DntFieldType.Percentage:
                                    bw.Write((float)val * 100f);
                                    break;
                                case DntFieldType.Float:
                                    bw.Write((float)val);
                                    break;
                            }
                        }
                    }
                    // 【核心补漏】在所有数据写完后，写入文件尾部标识
                    // 注意：如果原版文件在 THEND 前后还有其他字节（如长度占位符），需按其格式补齐
                    byte[] endBytes = GameEncoding.GetBytes("THEND");
                    bw.Write(endBytes);
                    // 【必须要加这一行】把尾部可能残留的最后几个字节强行刷新到 MemoryStream 中
                    bw.Flush();
                } // BinaryWriter 正常释放并刷新数据到 MemoryStream

                // 【核心更改 2】将组装好的明文二进制流，严格按照“绝对物理偏移量”全文件覆盖异或加密写入硬盘
                ms.Position = 0; // 将内存流指针重置到头部
                byte[] rawBytes = ms.ToArray(); // 转换为字节数组

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (isCyclic)
                    {
                        rawBytes = CyclicXorCryptoHelper.cyclicXorCrypt(rawBytes);
                    }
                    // 一次性写入加密后的全文件流
                    fs.Write(rawBytes, 0, rawBytes.Length);
                }
            }
        }

        public static void SaveToCsv(DntDocument doc, string filePath)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            string folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (StreamWriter sw = new StreamWriter(filePath, false, GameEncoding))
            {
                // 1. 写入表头 (包含固定列和动态列)
                List<string> headers = new List<string> { "PKID" };
                foreach (var field in doc.Fields)
                {
                    headers.Add(field.FieldName);
                }
                // 注意：这里我们顺便把字段类型记录在第二行，方便读取时还原（如果不需要可以删掉这行）
                sw.WriteLine(string.Join(",", headers.Select(EscapeCsv)));

                List<string> types = new List<string> { "Int32" };
                foreach (var field in doc.Fields)
                {
                    types.Add(field.FieldType.ToString());
                }
                sw.WriteLine(string.Join(",", types));

                // 2. 写入数据行
                var pkidList = doc.Columns["PKID"];

                for (int i = 0; i < doc.RecordCount; i++)
                {
                    List<string> rowFields = new List<string>();

                    // 写入固定列
                    rowFields.Add(pkidList[i]?.ToString() ?? string.Empty);

                    // 写入动态列
                    foreach (var field in doc.Fields)
                    {
                        var columnList = doc.Columns[field.FieldName];
                        object val = columnList[i];
                        rowFields.Add(val?.ToString() ?? string.Empty);
                    }

                    sw.WriteLine(string.Join(",", rowFields.Select(EscapeCsv)));
                }
            }
        }

        // CSV 安全转义函数
        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        public static DntDocument LoadFromCsv(string filePath)
        {
            DntDocument doc = new DntDocument();

            // 使用与游戏相同的编码读取 CSV
            string[] lines = File.ReadAllLines(filePath, GameEncoding);
            if (lines.Length < 2) throw new InvalidDataException("CSV 文件格式不正确或缺少数据。");

            // 1. 解析表头 (第一行) 和 类型 (第二行)
            List<string> headers = ParseCsvLine(lines[0]);
            List<string> types = ParseCsvLine(lines[1]);

            // 前两列固定为 ChineseTranslation 和 PKID
            // 剩下的列构建 Fields 列表
            for (int i = 1; i < headers.Count; i++)
            {
                if (Enum.TryParse(types[i], out DntFieldType fieldType))
                {
                    doc.Fields.Add(new DntFieldDescription
                    {
                        FieldName = headers[i],
                        FieldType = fieldType
                    });
                }
                else
                {
                    // 如果解析失败，默认给 Text
                    doc.Fields.Add(new DntFieldDescription
                    {
                        FieldName = headers[i],
                        FieldType = DntFieldType.Text
                    });
                }
            }

            doc.FieldCount = (ushort)doc.Fields.Count;
            doc.RecordCount = (uint)(lines.Length - 2); // 减去表头和类型行
            doc.NullBytes = 0; // 默认填充 0

            // 初始化列容器
            doc.InitializeColumnStorage();

            var pkidList = doc.Columns["PKID"] as List<uint>;

            // 2. 循环读取数据行
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                List<string> rowFields = ParseCsvLine(lines[i]);
                if (rowFields.Count < 2) continue;

                // 填充固定列
                pkidList.Add(uint.TryParse(rowFields[0], out uint pkid) ? pkid : 0);

                // 填充动态列
                for (int f = 0; f < doc.Fields.Count; f++)
                {
                    var field = doc.Fields[f];
                    var columnList = doc.Columns[field.FieldName];
                    string rawValue = rowFields.Count > (f + 1) ? rowFields[f + 1] : string.Empty;

                    // 根据定义的字段类型将 string 还原为强类型
                    switch (field.FieldType)
                    {
                        case DntFieldType.Text:
                            columnList.Add(rawValue);
                            break;
                        case DntFieldType.BooleanInt:
                            // 兼容 True/False 或 1/0 的转换
                            if (int.TryParse(rawValue, out int boolInt)) columnList.Add(boolInt);
                            else if (bool.TryParse(rawValue, out bool b)) columnList.Add(b ? 1 : 0);
                            else columnList.Add(0);
                            break;
                        case DntFieldType.Int32:
                            columnList.Add(int.TryParse(rawValue, out int intVal) ? intVal : 0);
                            break;
                        case DntFieldType.Percentage:
                            columnList.Add(float.TryParse(rawValue, out float PercentVal) ? PercentVal / 100f : 0f);
                            break;
                        case DntFieldType.Float:
                            columnList.Add(float.TryParse(rawValue, out float floatVal) ? floatVal : 0f);
                            break;
                        default:
                            columnList.Add(rawValue);
                            break;
                    }
                }
            }

            return doc;
        }

        // 简易的标准 CSV 行解析器 (处理带双引号包裹和逗号的文本)
        private static List<string> ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentField.Append('"'); // 转义的内部双引号 "" -> "
                            i++;
                        }
                        else
                        {
                            inQuotes = false; // 结束引号
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true; // 开始引号
                    }
                    else if (c == ',')
                    {
                        result.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }
            result.Add(currentField.ToString());
            return result;
        }
    }
}