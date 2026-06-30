using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    public enum DntFieldType : byte
    {
        Text = 1,          // 普通变长文本
        BooleanInt = 2,    // 布尔型（以32位整型存放）
        Int32 = 3,         // 32位整型
        Percentage = 4,    // 百分比（单精度浮点型，除以100）
        Float = 5          // 单精度浮点型
    }
}
