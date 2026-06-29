using System;
using System.IO;
using DntEditor_Hang.Helpers;

namespace DntEditor_Hang.Models
{
    /// <summary>
    /// 系统配置数据实体类
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// DNT 目录路径
        /// </summary>
        public static string DntPath { get; set; } = string.Empty;

        /// <summary>
        /// 快速保存（明文）目录路径
        /// </summary>
        public static string PlainPath { get; set; } = string.Empty;

        /// <summary>
        /// 保存密文目录路径
        /// </summary>
        public static string CipherPath { get; set; } = string.Empty;

        /// <summary>
        /// PAK 补丁目录路径
        /// </summary>
        public static string PakPath { get; set; } = string.Empty;

        /// <summary>
        /// 客户端目录路径
        /// </summary>
        public static string ClientPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否同时保存至 (明文/密文) 目录
        /// </summary>
        public static bool IsSyncSaveEnabled { get; set; } = false;

        /// <summary>
        /// 业务辅助方法：一键校验所有保存的路径是否在电脑中真实存在
        /// </summary>
        /// <returns>返回未通过验证的路径说明；若全部合法则返回 null</returns>
        public static bool ValidatePaths(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path)) return false;
            return true; // 全部验证通过
        }

        public static void Load()
        {
            // 从同目录下的 config.ini 读取数据（分为 "Paths" 和 "Settings" 两个小节）
            DntPath = IniHelper.Read("Paths", "DntPath", IniHelper.IniPath);
            PlainPath = IniHelper.Read("Paths", "PlainPath", IniHelper.IniPath);
            CipherPath = IniHelper.Read("Paths", "CipherPath", IniHelper.IniPath);
            PakPath = IniHelper.Read("Paths", "PakPath", IniHelper.IniPath);
            ClientPath = IniHelper.Read("Paths", "ClientPath", IniHelper.IniPath);

            // 读取布尔值（INI 只能存字符串，所以读出来需要转换）
            string syncSaveStr = IniHelper.Read("Settings", "IsSyncSaveEnabled", IniHelper.IniPath);
            IsSyncSaveEnabled = syncSaveStr.ToLower() == "true";
        }
        public static void Save()
        {
            // 2. 依次写入到同目录下的 config.ini 中
            IniHelper.Write("Paths", "DntPath", DntPath, IniHelper.IniPath);
            IniHelper.Write("Paths", "PlainPath", PlainPath, IniHelper.IniPath);
            IniHelper.Write("Paths", "CipherPath", CipherPath, IniHelper.IniPath);
            IniHelper.Write("Paths", "PakPath", PakPath, IniHelper.IniPath);
            IniHelper.Write("Paths", "ClientPath", ClientPath, IniHelper.IniPath);

            // 布尔值转为字符串存储
            IniHelper.Write("Settings", "IsSyncSaveEnabled", IsSyncSaveEnabled.ToString().ToLower(), IniHelper.IniPath);
        }
    }
}