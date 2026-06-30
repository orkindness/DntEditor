using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    public class translationDicts
    {
        // 定义常量后缀，防止各处拼写不一致
        public const string ParamSuffix = "Param";
        public const string TargetColumnKey = "ChineseTranslation";

        public const string SourcePath = "\\database\\";
        public const string UistringSource = "uistring";

        public const string MapSource = "maptable";
        public const string MonsterSource = "monstertable";
        public const string NPCSource = "npctable";
        public const string ItemSource = "itemtable";
        public const string SkillSource = "skilltable";
        public static Dictionary<string, string> translationDict;

        /// <summary>
        /// 智能解析游戏参数化文本，支持大括号嵌套再翻译
        /// </summary>
        /// <param name="templateText">母本模板，例如: "{0}级普通{1} {2}"</param>
        /// <param name="paramChain">原始参数链，例如: "24,{1000029331},{1000015539}"</param>
        /// <param name="transDict">翻译字典</param>
        /// <returns>最终渲染后的完整中文文本</returns>
        public static string FormatGameText(string templateText, string paramChain, Dictionary<string, string> transDict)
        {
            // 防御 1：如果母本为空，直接返回空
            if (string.IsNullOrEmpty(templateText)) return string.Empty;

            // 防御 2：如果参数链为空，说明不需要格式化，直接返回母本
            if (string.IsNullOrEmpty(paramChain)) return templateText;

            // 1. 切割参数链（按逗号拆分）
            string[] rawParams = paramChain.Split(new char[] { ',' }, StringSplitOptions.None);
            object[] finalArgs = new object[rawParams.Length];

            // 2. 遍历处理每一个参数
            for (int i = 0; i < rawParams.Length; i++)
            {
                string p = rawParams[i].Trim();

                // 判定：是否是被大括号包裹的嵌套 ID (例如 {1000029331})
                if (p.StartsWith("{") && p.EndsWith("}"))
                {
                    // 剥离外层大括号，提取出纯粹的 ID (1000029331)
                    string innerId = p.Substring(1, p.Length - 2).Trim();

                    // 防御性碰撞：去字典里二次检索该 ID 的翻译
                    if (transDict != null && transDict.TryGetValue(innerId, out string innerTranslation))
                    {
                        // 找到嵌套翻译，作为参数备用
                        finalArgs[i] = innerTranslation;
                    }
                    else
                    {
                        // 防御：若字典里找不到这个嵌套ID的翻译，保留其原始形态，防止信息丢失
                        finalArgs[i] = p;
                    }
                }
                else
                {
                    // 纯数字或纯普通文本，无需翻译，直接作为参数
                    finalArgs[i] = p;
                }
            }

            // 3. 终极防御：利用 string.Format 执行安全的动态拼装
            try
            {
                // finalArgs 的顺序将天然对应 {0}, {1}, {2}...
                return string.Format(templateText, finalArgs);
            }
            catch (FormatException)
            {
                // 防御：如果模板中的占位符(如含有{3})和参数个数对不上，string.Format 会崩溃
                // 此时采用保底策略，返回母本 + 附带参数，确保程序绝不闪退
                return $"{templateText} [参数错误: {paramChain}]";
            }
        }
        /// <summary>
        /// 模块化的游戏文本批量碰撞翻译函数（不依赖任何UI控件）
        /// </summary>
        /// <param name="document">要注入翻译的 DntDocument 实例</param>
        /// <param name="sourceColumnName">当前选中的 ID 列名（例如 "_NameID"）</param>
        /// <param name="transDict">翻译字典</param>
        /// <param name="formatProvider">提供 FormatGameText 格式化逻辑的对象或实例</param>
        /// <returns>返回实际处理成功的总行数。若出错则返回 -1</returns>
        public static int TranslateColumnData(DntDocument document, string sourceColumnName, Dictionary<string, string> transDict)
        {
            // 1. 【强防御性检查】前置核心对象非空校验
            if (document?.Columns == null || transDict == null || string.IsNullOrEmpty(sourceColumnName)) return -1;

            // 2. 【强防御性检查】确保目标“ChineseTranslation”列已在字典中初始化
            if (!document.Columns.TryGetValue(TargetColumnKey, out IList chineseTranslationList) || chineseTranslationList == null) return -1;

            // 3. 【强防御性检查】确保被翻译的源 ID 列存在
            if (!document.Columns.TryGetValue(sourceColumnName, out IList sourceColumnList) || sourceColumnList == null) return -1;

            // 4. 安全获取与之配套的参数列（例如 "_NameIDParam"）
            string paramColumnKey = sourceColumnName + ParamSuffix;
            document.Columns.TryGetValue(paramColumnKey, out IList paramColumnList);

            try
            {
                // 5. 核心处理前先清空旧的翻译数据
                chineseTranslationList.Clear();

                // 6. 执行批量碰撞
                for (int i = 0; i < sourceColumnList.Count; i++)
                {
                    object cellValue = sourceColumnList[i];
                    string nameIdKey = cellValue?.ToString()?.Trim() ?? string.Empty;

                    // 去字典里抓取母本模板（例如 "{0}级普通{1} {2}"）
                    if (!string.IsNullOrEmpty(nameIdKey) && transDict.TryGetValue(nameIdKey, out string templateText))
                    {
                        // 安全提取当前行的参数链（例如 "24,{1000029331}"）
                        string paramChain = string.Empty;
                        if (paramColumnList != null && i < paramColumnList.Count)
                        {
                            paramChain = paramColumnList[i]?.ToString() ?? string.Empty;
                        }

                        // 调用外部传入的智能格式化方法进行多层嵌套替换
                        string finalResultText = FormatGameText(templateText, paramChain, transDict);
                        chineseTranslationList.Add(finalResultText);
                    }
                    else
                    {
                        // 防御：字典未命中时填空，确保总行数不错位
                        chineseTranslationList.Add(string.Empty);
                    }
                }

                return sourceColumnList.Count; // 返回处理成功的总行数
            }
            catch (Exception)
            {
                // 模块化函数内部不建议直接弹出 MessageBox 破坏架构，此处选择向上抛出或记录日志
                throw;
            }
        }
    }
}
