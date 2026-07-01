using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace DntEditor_Hang.Helpers
{
    public static class GlobalHelper
    {
        // 获取当前软件运行所在的绝对目录路径（末尾统一确保带有反斜杠 \）
        public static readonly string AppRootPath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 读取多个 INI 文件，将数据拼接合并后保存到新文件
        /// </summary>
        /// <param name="sourceFiles">要合并的源 INI 文件路径列表</param>
        /// <param name="targetFile">合并后的目标保存路径</param>
        public static void MergeIniFiles(List<string> sourceFiles, string targetFile)
        {
            // 内存数据库：Dictionary<节点名, Dictionary<键名, 键值>>
            var mergedData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // 1. 循环读取并解析每一个 INI 文件
            foreach (string filePath in sourceFiles)
            {
                if (!File.Exists(filePath)) continue; // 跳过不存在的文件

                string currentSection = ""; // 记录当前正在读取的节点名

                // 逐行读取文件
                foreach (string line in File.ReadLines(filePath, Encoding.UTF8))
                {
                    string trimmedLine = line.Trim();

                    // 跳过空行和注释行（以 ; 或 # 开头的行）
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    // 解析节点 [SectionName]
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();

                        if (!mergedData.ContainsKey(currentSection))
                        {
                            mergedData[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        continue;
                    }

                    // 解析键值对 Key=Value
                    int equalSignIndex = trimmedLine.IndexOf('=');
                    if (equalSignIndex > 0 && !string.IsNullOrEmpty(currentSection))
                    {
                        string key = trimmedLine.Substring(0, equalSignIndex).Trim();
                        string value = trimmedLine.Substring(equalSignIndex + 1).Trim();

                        // 写入内存（如果键已存在，后面的文件内容会覆盖/更新前面的内容）
                        mergedData[currentSection][key] = value;
                    }
                }
            }

            // 2. 将合并后的内存数据统一写入目标文件
            using (StreamWriter writer = new StreamWriter(targetFile, false, Encoding.UTF8))
            {
                foreach (var section in mergedData)
                {
                    // 写入节点名
                    writer.WriteLine($"[{section.Key}]");

                    // 写入该节点下的所有键值对
                    foreach (var kvp in section.Value)
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                    }

                    // 节点之间空一行，保持排版美观
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// 加载ini文件转字典类型
        /// </summary>
        /// <param name="iniFilePath"></param>
        /// <returns></returns>
        public static Dictionary<string, string> LoadIniTranslation(string iniFilePath)
        {
            // 1. 创建字典用于存储键值对 (不区分大小写的键，防止匹配时因为空格或大小写对不上)
            var translationDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 2. 安全检查
            if (!File.Exists(iniFilePath))
            {
                //throw new FileNotFoundException($"找不到指定的 INI 翻译文件: {iniFilePath}");
                return null;
            }

            // 3. 采用 UTF-8 编码逐行读取，防止大文件一次性读入导致内存卡顿
            using (FileStream fs = new FileStream(iniFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // 4. 清除行两边的空格
                    line = line.Trim();

                    // 5. 过滤掉空白行、INI小节名(如 [UI_STRINGS])、以及注释行（以 ; 或 // 开头）
                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith("[") ||
                        line.StartsWith(";") ||
                        line.StartsWith("//"))
                    {
                        continue;
                    }

                    // 6. 寻找第一个等号的位置
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        // 提取等号左边的 键 (ID)
                        string key = line.Substring(0, separatorIndex).Trim();

                        // 提取等号右边的 值为 翻译文本 (使用 Substring 确保即使文本里包含 "=" 也能完整保留)
                        string value = line.Substring(separatorIndex + 1).Trim();

                        // 7. 处理之前转义过的换行符 \n，恢复为真实的换行（视你后续界面显示需求而定）
                        value = value.Replace("\\n", Environment.NewLine);

                        // 8. 存入字典（如果出现重复的Key，后面的覆盖前面的，防止闪退）
                        translationDict[key] = value;
                    }
                }
            }

            return translationDict;
        }

        /// <summary>
        /// 将xml转ini
        /// </summary>
        /// <param name="xmlFilePath"></param>
        /// <param name="iniFilePath"></param>
        /// <returns></returns>
        public static bool ConvertXmlToIni(string xmlFilePath, string iniFilePath)
        {
            // 1. 安全检查
            if (!File.Exists(xmlFilePath))
            {
                //throw new FileNotFoundException($"找不到源 XML 文件: {xmlFilePath}");
                return false;
            }

            Encoding fileEncoding = Encoding.UTF8;
            StringBuilder iniBuilder = new StringBuilder();
            //iniBuilder.AppendLine("[UI_STRINGS]");

            // 2. 将整个 XML 文件作为纯文本读取到内存中
            string xmlContent = File.ReadAllText(xmlFilePath, fileEncoding);

            // 3. 【核心修复】使用正则表达式，将所有的 XML 注释 (<!-- ... -->) 彻底清除干净
            // 这样无论注释里有多少个 "--"，在进入 XML 解析器前就已经被全部删掉了
            xmlContent = Regex.Replace(xmlContent, @"<!--[\s\S]*?-->", string.Empty);

            // 4. 使用 XmlDocument 解析清洗干净后的文本（改用 LoadXml 替代 Load）
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xmlContent);
            }
            catch (XmlException ex)
            {
                // 容错处理：如果还有其他 XML 语法错误，抛出更清晰的提示
                throw new Exception($"XML 语法清洗后仍有错误，请检查文件结构。错误原因: {ex.Message}");
                return false;
            }

            // 5. 定位并提取 <message> 节点
            XmlNodeList messageNodes = xmlDoc.SelectNodes("//message");

            if (messageNodes != null)
            {
                foreach (XmlNode node in messageNodes)
                {
                    string mid = node.Attributes?["mid"]?.Value;
                    string text = node.InnerText;

                    if (!string.IsNullOrEmpty(mid))
                    {
                        text = text.Trim();
                        // 将真实的换行符替换为转义的 \n，保证 INI 文件单行格式
                        text = text.Replace("\r", "").Replace("\n", "\\n");

                        iniBuilder.AppendLine($"{mid}={text}");
                    }
                }
            }

            // 6. 写入到目标 INI 文件中
            string directory = Path.GetDirectoryName(iniFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(iniFilePath, iniBuilder.ToString(), fileEncoding);
            return true;
        }

        /// <summary>
        /// 从ini文件获取翻译列表
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
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
