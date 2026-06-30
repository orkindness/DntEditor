using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Helpers
{
    public class CyclicXorCryptoHelper
    {
        // 定义 4 字节的固定密钥序列（十六进制：0x87, 0x54, 0x36, 0x12）
        private static readonly byte[] Key = new byte[] { 0x87, 0x54, 0x36, 0x12 };

        /// <summary>
        /// 文件转文件加密
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="isCrypto"></param>         true:加密操作 false:解密操作
        public static bool ProcessFile(string inputFilePath, string outputFilePath,bool isCrypto=false)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("源文件不存在，请检查路径。");

            // ======= 核心改造：前 4 字节明密文智能识别 =======
            bool isEncryptedFile = false;

            // 以只读、共享模式打开文件，专门检查前 4 字节
            using (FileStream fsCheck = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] header = new byte[4];
                int headerBytesRead = fsCheck.Read(header, 0, header.Length);

                // 如果文件至少有 4 字节，则根据规则判断
                if (headerBytesRead == 4)
                {
                    // 规则：如果是明文，前 4 字节全为 0。
                    // 只要任意一个字节不是 0，就说明它是【密文文件】
                    isEncryptedFile = header[0] != 0 || header[1] != 0 || header[2] != 0 || header[3] != 0;
                }
            }

            // ======= 拦截逻辑 =======
            // 1. 用户想【加密】(isCrypto为true)，但文件【已经是密文】 -> 直接返回
            if (isCrypto && isEncryptedFile)
            {
                return false;
            }

            // 2. 用户想【解密】(isCrypto为false)，但文件【已经是明文】 -> 直接返回
            if (!isCrypto && !isEncryptedFile)
            {
                return false;
            }
            // ================================================

            // 每次读取 64KB 的缓冲区，平衡内存与磁盘 I/O 效率
            byte[] buffer = new byte[64 * 1024];

            // 关键计数器：记录当前处理的字节在整个文件中的【绝对物理偏移量】
            long absoluteOffset = 0;

            using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream fsOutput = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                int bytesRead;
                // 循环分块读取文件
                while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        // 核心规则：根据当前字节的【绝对物理偏移量】对密钥长度(4)取模，决定选取哪个密钥字节
                        int keyIndex = (int)(absoluteOffset % Key.Length);

                        // 执行异或操作
                        buffer[i] = (byte)(buffer[i] ^ Key[keyIndex]);

                        // 绝对物理偏移量累加
                        absoluteOffset++;
                    }

                    // 将处理后的缓冲区写入新文件
                    fsOutput.Write(buffer, 0, bytesRead);
                }
            }
            return true;
        }
        public static byte[] cyclicXorCrypt(byte[] rawBytes)
        {
            // 遍历每个字节，此时的 i 完美对应文件中的【绝对物理偏移量 (Offset)】
            for (int i = 0; i < rawBytes.Length; i++)
            {
                // 核心规则：偏移量对密钥长度取模
                int keyIndex = i % Key.Length;

                // 执行循环异或加密
                rawBytes[i] = (byte)(rawBytes[i] ^ Key[keyIndex]);
            }
            return rawBytes;
        }
    }
}
