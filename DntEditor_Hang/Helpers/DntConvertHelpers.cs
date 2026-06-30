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
                                columnList.Add(br.ReadInt32() != 0);
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
                                    bw.Write((bool)val ? 1 : 0);
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
    }
}