using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Helpers
{
    public static class GlobalHelper
    {
        // 获取当前软件运行所在的绝对目录路径（末尾统一确保带有反斜杠 \）
        public static readonly string AppRootPath = AppDomain.CurrentDomain.BaseDirectory;
        public static Dictionary<string, string> GetTranslationDict(string path,string fileName)
        {
            // 2. 定位并读取翻译 INI 文件 (假设翻译文件放在软件同级目录下)
            string translationIniPath = Path.Combine(path, fileName);
            Dictionary<string, string> translationDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(translationIniPath))
            {

                // 核心修复 2：使用 StreamLines + 自动检测编码（detectEncodingFromByteOrderMarks: true）
                // 并强制降级 fallback 使用 GB2312 编码（即 Windows 默认的简体中文 ANSI）
                var gb2312Encoding = System.Text.Encoding.GetEncoding("GB2312");
                using (StreamReader reader = new StreamReader(translationIniPath, gb2312Encoding, true))
                {
                    // 逐行读取 INI 文件，建立快速查找字典
                    string rawLine;
                    while ((rawLine = reader.ReadLine()) != null)
                    {
                        // 核心修复 1：先用 Trim() 去除行首行尾看不见的空格、换行符(\r\n)
                        string line = rawLine.Trim();

                        // 过滤空行、注释或不包含等号的无效行
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("[") || !line.Contains("="))
                            continue;

                        int eqIndex = line.IndexOf('=');
                        string key = line.Substring(0, eqIndex).Trim();       // 比如: item.dnt
                        string value = line.Substring(eqIndex + 1).Trim();   // 比如: 道具表

                        // 防止 INI 里有重复的 Key 导致程序崩溃
                        if (!translationDict.ContainsKey(key))
                        {
                            translationDict.Add(key, value);
                        }
                    }
                }
            }
            return translationDict;
        }
        /// <summary>
        /// 选择文件夹方法
        /// </summary>
        /// <param name="title"></param>
        /// <param name="initialPath"></param>
        /// <returns></returns>
        public static string SelectFolder(string title, string initialPath = "")
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.Title = title;
                dialog.IsFolderPicker = true; // 关键：设置为选择文件夹，而不是选择文件
                dialog.AddToMostRecentlyUsedList = false;
                dialog.AllowNonFileSystemItems = false;
                dialog.EnsureFileExists = true;
                dialog.EnsurePathExists = true;
                dialog.EnsureReadOnly = false;
                dialog.EnsureValidNames = true;
                dialog.Multiselect = false;
                dialog.ShowPlacesList = true; // 显示左侧快捷导航（下载、桌面、此电脑等）

                // 核心设置：如果传入的路径不为空，且在电脑里真实存在，则作为默认打开路径
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.InitialDirectory = initialPath;
                }
                else
                {
                    // 如果没传路径或路径失效，可以设置一个保底的默认路径:软件所在目录
                    dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }
        /// <summary>
        /// 选择文件弹窗
        /// </summary>
        /// <param name="title"></param>标题
        /// <param name="rawDisplayName"></param>显示内容
        /// <param name="extensionList"></param>过滤规则
        /// <param name="initialPath"></param>初始化路径
        /// <returns></returns>
        public static string SelectFile(string title, string rawDisplayName, string extensionList, string initialPath = "")
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.Title = title;

                // 核心修改 1：关闭文件夹选择模式，切换为文件选择模式
                dialog.IsFolderPicker = false;

                // 核心修改 2：添加文件后缀名过滤器，只允许用户选择 .dnt 文件
                //dialog.Filters.Add(new CommonFileDialogFilter("DNT数据文件 (*.dnt)", "*.dnt"));
                dialog.Filters.Add(new CommonFileDialogFilter(rawDisplayName, extensionList));
                // 如果想让用户也能切换查看“所有文件”，可以取消下面这行的注释：
                // dialog.Filters.Add(new CommonFileDialogFilter("所有文件 (*.*)", "*.*"));

                dialog.AddToMostRecentlyUsedList = false;
                dialog.AllowNonFileSystemItems = false;
                dialog.EnsureFileExists = true; // 确保用户选择的文件必须真实存在
                dialog.EnsurePathExists = true;
                dialog.EnsureReadOnly = false;
                dialog.EnsureValidNames = true;
                dialog.Multiselect = false;
                dialog.ShowPlacesList = true;

                // 核心修改 3：因为是寻找文件，保底的路径检查建议兼容“文件夹路径”或“直接定位到文件所在的目录”
                if (!string.IsNullOrWhiteSpace(initialPath))
                {
                    // 如果传入的是完整文件路径，先提取其所在的文件夹目录
                    string directory = Directory.Exists(initialPath) ? initialPath : Path.GetDirectoryName(initialPath);

                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                    }
                    else
                    {
                        dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    }
                }
                else
                {
                    dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // 返回用户选择的 .dnt 文件的完整绝对路径
                    return dialog.FileName;
                }
            }
            return null;
        }
    }
}
