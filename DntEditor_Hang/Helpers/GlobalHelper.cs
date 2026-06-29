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
    }
}
