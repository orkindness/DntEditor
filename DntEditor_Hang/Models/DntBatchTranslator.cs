using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DntEditor_Hang.Models; // 确保引用了你的实体命名空间
using DntEditor_Hang.Helpers;

public static class DntBatchTranslator
{
    // 定义常量配置，确保全程序统一
    private const string TargetIdColumn = "_NameID";
    private const string MapTargetIdColumn = "_MapNameID";
    private const string PkidColumnKey = "PKID";
    private const string ChineseTranslationKey = "ChineseTranslation";
    private const string IniSectionHeader = "[TRANSLATIONS]";

    /// <summary>
    /// 自动化批量翻译同类型DNT文件并合并导出到对应的INI
    /// </summary>
    /// <param name="folderPath">存放 .dnt 文件的文件夹路径</param>
    /// <param name="uistringIniPath">uistring.ini 翻译源文件的路径</param>
    public static void ExecuteBatchTranslation(string folderPath, string uistringIniPath,List<string> groupList, translationDicts dicts)
    {
        // 1. 防御性检查
        if (!Directory.Exists(folderPath))
            //throw new DirectoryNotFoundException($"找不到DNT文件夹路径: {folderPath}");
            
            return;
        if (!File.Exists(uistringIniPath))
            return;

        // 2. 自动加载 uistring.ini 翻译字典
        // 此处调用你之前编写的加载函数（假设已将其封装，此处直接提取逻辑或直接调用）
        Dictionary<string, string> translationDict = LoadIniTranslationInternal(uistringIniPath);

        // 3. 获取文件夹下所有的 .dnt 文件
        string[] dntFiles = Directory.GetFiles(folderPath, "*.dnt", SearchOption.TopDirectoryOnly);
        if (dntFiles.Length == 0) return;

        // 4. 【核心创新】对 DNT 文件进行“同类型”自动分组
        // 规则：根据第一个下划线"_"前的内容作为大类名字，例如 "itemtable.dnt" 和 "itemtable_abyss.dnt" 都会归为 "itemtable"
        var fileGroups = dntFiles.GroupBy(filePath =>
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            int underscoreIndex = fileNameWithoutExt.IndexOf('_');
            // 如果包含下划线，取下划线前的部分；否则取整个文件名
            return underscoreIndex > 0 ? fileNameWithoutExt.Substring(0, underscoreIndex).ToLower() : fileNameWithoutExt.ToLower();
        });

        // 5. 遍历每一个大类分组（例如 itemtable 分组，里面包含 itemtable.dnt, itemtable_abyss.dnt 等）
        foreach (var group in fileGroups)
        {
            string baseTypeName = group.Key; // 例如 "itemtable"

            //添加一个判断,itemtable\maptable\monstertable\npctable\skilltable
            if (groupList==null || groupList.Count==0) return;
            bool isOper = false;
            foreach (string item in groupList)
            {
                if (baseTypeName == item)
                {
                    isOper = true;
                    break;
                }
            }
            if (!isOper) continue;
            // 使用 Dictionary 存储当前组所有文件合并去重后的 PKID -> ChineseTranslation 结果
            // 不区分大小写，防止Key冲突
            var mergedResultDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 遍历当前组内的每一个具体的 .dnt 文件
            foreach (string dntFilePath in group)
            {
                try
                {
                    // A. 调用你最早写的内存安全加载方法加载单文件
                    DntDocument doc = DntConvertHelpers.LoadFromFile(dntFilePath);
                    if (doc == null || doc.Columns == null) continue;

                    // B. 确保目标翻译存储列在内存中已被正确初始化
                    if (!doc.Columns.ContainsKey(ChineseTranslationKey))
                    {
                        doc.Columns.Add(ChineseTranslationKey, new List<string>((int)doc.RecordCount));
                    }

                    // C. 调用你上一步优化好的模块化函数进行数据碰撞翻译
                    // 这一步会把最终拼装好的文本填入到 doc.Columns["ChineseTranslation"] 中

                    int processedRows = dicts.TranslateColumnData(doc, TargetIdColumn, translationDict);

                    // 如果由于该文件没有 _NameID 列导致处理失败(-1)，则跳过此文件
                    if (processedRows < 0)
                    {
                        processedRows = dicts.TranslateColumnData(doc, MapTargetIdColumn, translationDict);
                    }
                    if (processedRows < 0) continue;
                    // D. 提取 PKID 和 ChineseTranslation 结果并入组字典
                    var pkidList = doc.Columns[PkidColumnKey] as List<uint>;
                    var chineseList = doc.Columns[ChineseTranslationKey] as List<string>;

                    if (pkidList != null && chineseList != null)
                    {
                        for (int i = 0; i < pkidList.Count; i++)
                        {
                            string pkidStr = pkidList[i].ToString();
                            string translatedText = chineseList[i] ?? string.Empty;

                            // 写入合并字典：如果不同文件里有重复的 PKID，后者覆盖前者（去重）
                            mergedResultDict[pkidStr] = translatedText;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 防御性设计：单个文件若损坏或报错，不应该卡死整个批量流水线
                    // 此处可以记录日志，提示哪个文件损坏了
                    Console.WriteLine($"处理文件时发生跳过错误 [{Path.GetFileName(dntFilePath)}]: {ex.Message}");
                }
            }

            // 6. 将当前大类合并后的字典数据，安全写入对应的目标 .ini 文件
            if (mergedResultDict.Count > 0)
            {
                string targetIniPath = Path.Combine(GlobalHelper.AppRootPath+translationDicts.SourcePath, $"{baseTypeName}.ini");
                WriteMergedDictToIni(targetIniPath, mergedResultDict);
            }
        }
    }

    /// <summary>
    /// 内部保底方法：高性能将合并后的键值对存入指定 INI
    /// </summary>
    public static void WriteMergedDictToIni(string targetIniPath, Dictionary<string, string> dict)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(IniSectionHeader);
        string fpath = Path.GetDirectoryName(targetIniPath);
        if (!string.IsNullOrEmpty(fpath) && !Directory.Exists(fpath))
        {
            Directory.CreateDirectory(fpath);
        }
        foreach (var kvp in dict)
        {
            // 防御：若翻译结果包含换行符，转换为 \n 转义，防止破坏 INI 结构
            string cleanValue = kvp.Value.Replace("\r", "").Replace("\n", "\\n");
            sb.AppendLine($"{kvp.Key}={cleanValue}");
        }

        // 使用 UTF-8 编码安全写入磁盘
        File.WriteAllText(targetIniPath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 内部保底方法：加载并生成字典（采用前面编写的高性能流式逻辑）
    /// </summary>
    private static Dictionary<string, string> LoadIniTranslationInternal(string iniFilePath)
    {
        var translationDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (FileStream fs = new FileStream(iniFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("[") || line.StartsWith(";") || line.StartsWith("//")) continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    translationDict[key] = value;
                }
            }
        }
        return translationDict;
    }
}