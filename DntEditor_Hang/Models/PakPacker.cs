using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    class PakPacker
    {
        // 定义文件信息块结构
        private class FileRecord
        {
            public string RelativePath;      // 相对路径
            public uint PackedSize;          // 压缩后大小
            public uint TrueSize;            // 实际大小
            public uint OccupationSize;      // 在包内占据的大小（通常等于PackedSize）
            public uint Offset;              // 在包内的偏移
            public uint Flag = 0;            // 标记
        }

        public static void PackFolderToPak(string sourceFolderPath, string outputPakPath)
        {
            // 【修改这里】：直接删掉 Encoding.RegisterProvider 那两行，改用下面这一行即可
            Encoding euckrEncoding = Encoding.GetEncoding(949);

            // 1. 遍历选定路径下所有的文件，提取相对路径
            string cleanedFolder = sourceFolderPath;
            string[] allFiles = Directory.GetFiles(cleanedFolder, "*.*", SearchOption.AllDirectories);

            List<FileRecord> records = new List<FileRecord>();

            // 2. 创建 PAK 文件
            using (FileStream fs = new FileStream(outputPakPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // 写入空白头部（共 1024 字节）先占位
                byte[] emptyHeader = new byte[1024];
                bw.Write(emptyHeader);

                // 3. 读取每一个待压缩文件并写入中段
                foreach (string file in allFiles)
                {
                    // 计算相对路径 (去掉根目录和开头的斜杠)
                    string relativePath = file.Substring(cleanedFolder.Length);
                    // 将路径中的反斜杠统一替换为打包常见的标准斜杠（可根据游戏实际要求调整）
                    //relativePath = relativePath.Replace('\\', '/');

                    FileInfo fileInfo = new FileInfo(file);
                    FileRecord record = new FileRecord
                    {
                        RelativePath = relativePath,
                        TrueSize = (uint)fileInfo.Length,
                        Offset = (uint)fs.Position // 当前位置即为文件在包内的偏移
                    };

                    // 读取原始文件数据
                    byte[] rawData = File.ReadAllBytes(file);

                    // 调用 zlib 压缩 (普通压缩，带 78 01 头部)
                    byte[] compressedData = CompressZlib(rawData);

                    record.PackedSize = (uint)compressedData.Length;
                    record.OccupationSize = record.PackedSize; // 默认占据大小等于压缩后大小

                    // 4. 将压缩后的数据保存到 PAK 文件
                    bw.Write(compressedData);

                    // 保存记录供尾部使用
                    records.Add(record);
                }

                // 5. 压缩数据完成后，记录当前位置（即第一个尾部文件信息块的偏移）
                uint recordOffset = (uint)fs.Position;
                uint recordNum = (uint)records.Count;

                // 6. 将所有的文件信息块保存到 PAK 尾部
                foreach (var record in records)
                {
                    long startPos = fs.Position;

                    // char szFilename[256]
                    byte[] nameBytes = euckrEncoding.GetBytes(record.RelativePath);
                    byte[] nameBuffer = new byte[256];
                    Array.Copy(nameBytes, nameBuffer, Math.Min(nameBytes.Length, 256));
                    bw.Write(nameBuffer);

                    // DWORD
                    bw.Write(record.PackedSize);
                    bw.Write(record.TrueSize);
                    bw.Write(record.OccupationSize);
                    bw.Write(record.Offset);
                    bw.Write(record.Flag);

                    // BYTE dbReserved[40]
                    byte[] reserved = new byte[40];
                    bw.Write(reserved);

                    // 校验大小是否正好为 316 字节
                    if (fs.Position - startPos != 316)
                    {
                        throw new Exception("文件信息块字节大小计算错误！");
                    }
                }

                // 7. 定位 PAK 文件指针到头部，再次写入正式头部块
                fs.Position = 0;

                // char szID[256] - 这里通常填游戏特有的魔数或ID标识，例如 "EyedentityGames Packing File"
                byte[] idBytes = Encoding.ASCII.GetBytes("EyedentityGames Packing File 0.1");
                byte[] idBuffer = new byte[256];
                Array.Copy(idBytes, idBuffer, Math.Min(idBytes.Length, 256));
                bw.Write(idBuffer);

                // DWORD dwVer (版本号或识别码，例如 1 或者根据DN版本定)
                bw.Write((uint)0.1);
                // DWORD dwRecordNum
                bw.Write(recordNum);
                // DWORD dwRecordOffset
                bw.Write(recordOffset);

                // BYTE dbReserved[756]
                byte[] headerReserved = new byte[756];
                bw.Write(headerReserved);
            }
        }

        /// <summary>
        /// 手动构建符合 zlib 格式（78 01 开头 + Adler32 校验）的压缩函数
        /// </summary>
        private static byte[] CompressZlib(byte[] input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // 1. 写入 zlib 头部：78 01 (无压缩/低压缩率常亮，或普通压缩)
                ms.WriteByte(0x78);
                ms.WriteByte(0x01);

                // 2. 写入 Deflate 压缩流
                using (DeflateStream deflate = new DeflateStream(ms, CompressionMode.Compress, true))
                {
                    deflate.Write(input, 0, input.Length);
                }

                // 3. 计算并写入 Adler-32 校验和 (zlib 格式要求尾部 4 字节为 Adler32)
                uint adler = CalculateAdler32(input);
                byte[] adlerBytes = BitConverter.GetBytes(adler);
                if (BitConverter.IsLittleEndian)
                {
                    // zlib 的校验和要求是大端序 (Big-Endian)
                    Array.Reverse(adlerBytes);
                }
                ms.Write(adlerBytes, 0, adlerBytes.Length);

                return ms.ToArray();
            }
        }

        // Adler-32 算法实现
        private static uint CalculateAdler32(byte[] data)
        {
            uint a = 1, b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }
    }
}
