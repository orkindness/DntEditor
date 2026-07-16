using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    // 1. 判断运算符枚举
    public enum CompareOperator
    {
        [Description("等于")] Equals,
        [Description("大于")] GreaterThan,
        [Description("小于")] LessThan,
        [Description("大于等于")] GreaterThanOrEqual,
        [Description("小于等于")] LessThanOrEqual
    }

    // 2. 操作运算符枚举
    public enum CalcOperator
    {
        [Description("等于")] Equals,
        [Description("加")] Add,
        [Description("减")] Subtract,
        [Description("乘")] Multiply,
        [Description("除")] Divide,
        [Description("等差数列")] ArithmeticProgression
    }
}
