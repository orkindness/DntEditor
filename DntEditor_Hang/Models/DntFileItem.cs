using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    public class DntFileItem
    {
        // 第一列：序列
        public int Index { get; set; }

        // 第二列：文件名称（包括后缀，如 item.dnt）
        public string FileName { get; set; }

        // 第三列：中文名称（从 ini 获取的翻译，如果没有则显示“未翻译”）
        public string ChineseName { get; set; }
    }
}
