using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DntEditor_Hang.Helpers
{
    public static class IniHelper
    {

        // 获取程序运行的同级目录下的 config.ini 路径
        public static readonly string IniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        // 引入 Windows 底层读取 INI 的 API
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // 引入 Windows 底层写入 INI 的 API
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        /// <summary>
        /// 写入 INI 配置（字符串）
        /// </summary>
        public static void Write(string section, string key, string value,string path)
        {
            WritePrivateProfileString(section, key, ToRelative(value), path);
        }
        public static void WriteWithoutRelative(string section, string key, string value, string path)
        {
            WritePrivateProfileString(section, key, value, path);
        }
        /// <summary>
        /// 读取 INI 配置（字符串）
        /// </summary>
        public static string Read(string section, string key, string path)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, "", sb, 1024, path);
            return ToAbsolute(sb.ToString());
        }
        /// <summary>
        /// 读取解析：将 INI 文件中的相对路径 (./) 还原为绝对路径
        /// </summary>
        public static string ToAbsolute(string iniPath)
        {
            if (string.IsNullOrWhiteSpace(iniPath)) return string.Empty;

            string trimmedPath = iniPath.Trim();

            // 如果是以 ./ 或 .\ 开头，进行替换还原
            if (trimmedPath.StartsWith("./") || trimmedPath.StartsWith(".\\"))
            {
                // 去掉前面的符号，拿到后面的子路径
                string subPath = trimmedPath.Substring(2);

                // 拼接软件绝对根目录，并格式化为标准的 Windows 路径
                return Path.GetFullPath(Path.Combine(GlobalHelper.AppRootPath, subPath));
            }

            return trimmedPath; // 如果本来就是绝对路径（如 D:\Test），原样返回
        }

        /// <summary>
        /// 存储简化：将绝对路径压缩为以 ./ 开头的相对路径
        /// </summary>
        public static string ToRelative(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) return string.Empty;

            string absPath = Path.GetFullPath(absolutePath.Trim());
            string appPath = Path.GetFullPath(GlobalHelper.AppRootPath);

            // 统一确保结尾有分隔符，防止因文件夹名字前缀相似导致误判（如 D:\App 和 D:\AppNew）
            if (!appPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                appPath += Path.DirectorySeparatorChar;
            }

            // 核心判断：如果用户选择的绝对路径，确实是以软件所在路径开头的
            if (absPath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
            {
                // 裁剪掉相同的软件根路径部分
                string relativePart = absPath.Substring(appPath.Length);

                // 用 ./ 拼接并返回（统一转换为正斜杠，INI 文件阅读体验更好）
                return "./" + relativePart.Replace('\\', '/');
            }

            return absPath; // 如果跟软件不在同一个目录下（比如软件在C盘，路径在D盘），必须原样保存绝对路径
        }
    }
}