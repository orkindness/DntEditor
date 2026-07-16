using DntEditor_Hang.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class CalculatorForm : Form
    {
        private MainForm mainForm = null;
        private List<int> rowtList = null;
        private List<DntFieldDescription> fieldList = null;

        // 用于防止给 DataSource 赋值时导致 TextChanged 陷入死循环的开关
        private bool isUpdatingSource = false;
        public CalculatorForm(MainForm mForm)
        {
            InitializeComponent();
            mainForm = mForm;
            initializeCalculator();
            //为操作符combobox赋值
            BindEnumToComboBox(comboBox4, typeof(CompareOperator));
            BindEnumToComboBox(comboBox5, typeof(CalcOperator));
            BindEnumToComboBox(comboBox7, typeof(CompareOperator));
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;
            comboBox7.SelectedIndex = 0;

            comboBox1.TextChanged += RowComboBox_TextChanged;
            comboBox2.TextChanged += RowComboBox_TextChanged;
            /***
             * // 获取当前选中的判断运算符
                if (cboCompare.SelectedValue is CompareOperator selectedCompare)
                {
                    switch (selectedCompare)
                    {
                        case CompareOperator.Equals:
                            // 执行等于的逻辑...
                            break;
                        case CompareOperator.GreaterThan:
                            // 执行大于的逻辑...
                            break;
                    }
                }
             * 
             * ***/
        }
        private void initializeCalculator()
        {
            //防御代码
            if (mainForm._currentDocument == null) return;

            // 2. 利用 LINQ 提取所有选中单元格的 RowIndex，并直接找出最小值和最大值
            int minRowIndex = mainForm.dataGridView1.SelectedCells.Cast<DataGridViewCell>().Min(c => c.RowIndex) + 1;
            int maxRowIndex = mainForm.dataGridView1.SelectedCells.Cast<DataGridViewCell>().Max(c => c.RowIndex) + 1;

            int columnIndex = mainForm.dataGridView1.CurrentCell.ColumnIndex;

            //获取表头列
            fieldList = new List<DntFieldDescription>(mainForm._currentDocument.Fields.ToArray());
            fieldList.Insert(0, new DntFieldDescription
            {
                FieldName = "PKID",
                FieldType = DntFieldType.Int32
            });
            //赋值列combobox3,6
            comboBox3.DataSource = fieldList.ToArray();
            comboBox3.DisplayMember = "FieldName";
            comboBox3.ValueMember = "FieldName";

            comboBox6.DataSource = fieldList.ToArray();
            comboBox6.DisplayMember = "FieldName";
            comboBox6.ValueMember = "FieldName";
            if (columnIndex < 1)
            {
                comboBox3.SelectedIndex = 0;
                comboBox6.SelectedIndex = 0;
            }
            else
            {
                comboBox3.SelectedIndex = columnIndex - 1;
                comboBox6.SelectedIndex = columnIndex - 1;
            }

            //获取行数列
            rowtList = new List<int>();
            for (int i = 1; i <= mainForm.dList.Count; i++)
            {
                rowtList.Add(i);
            }
            //行
            if (minRowIndex <= 0)
            {
                comboBox1.Text = 1.ToString();
                comboBox2.Text = 1.ToString();
            }
            else
            {
                comboBox1.Text = (minRowIndex).ToString();
                comboBox2.Text = (maxRowIndex).ToString();
            }

            RowComboBox_TextChanged(comboBox1, null);
            RowComboBox_TextChanged(comboBox2, null);

        }
        public static void BindEnumToComboBox(ComboBox comboBox, Type enumType)
        {
            if (!enumType.IsEnum) throw new ArgumentException("类型必须是枚举值");

            // 利用 LINQ 读取枚举的每一个项，并提取其 Description 特性
            var dataSource = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(e => new
                {
                    DisplayText = GetEnumDescription(e), // 界面显示的中文
                Value = e                             // 后台实际对应的枚举对象
            })
                .ToList();

            // 进行数据绑定
            comboBox.DataSource = dataSource;
            comboBox.DisplayMember = "DisplayText"; // 告诉 ComboBox 显示 DisplayText 属性
            comboBox.ValueMember = "Value";         // 告诉 ComboBox 后台值是 Value 属性
        }

        // 辅助方法：获取枚举的 Description 特性内容
        private static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            // 如果写了 Description 就返回中文，没写就返回枚举名本身
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.panel1.Visible = !panel1.Visible;
            if (panel1.Visible)
            {
                this.Height += panel1.Height;
            }
            else
            {
                this.Height -= panel1.Height;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.panel4.Visible = !panel4.Visible;
            if (panel4.Visible)
            {
                this.Height += panel4.Height;
            }
            else
            {
                this.Height -= panel4.Height;
            }
        }

        private void CalculatorForm_Load(object sender, EventArgs e)
        {
            // 检查是否有所有者（即主窗体）
            if (this.Owner != null)
            {
                // 核心公式：子窗口坐标 = 主窗口坐标 + (主窗口宽高 - 子窗口宽高) / 2
                int left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                int top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;

                this.Location = new Point(left, top);
            }
        }
        /// <summary>
        /// 安全更新数据源并保留用户光标位置
        /// </summary>
        private void UpdateComboDataSource(ComboBox combo,List<int> newList, string previousText)
        {
            try
            {
                isUpdatingSource = true; // 开启防死循环锁

                int selectionStart = combo.SelectionStart; // 记住光标位置

                combo.DataSource = newList; // 绑定合并后的 200 个数据

                combo.Text = previousText; // 还原输入框文本
                combo.SelectionStart = selectionStart; // 还原光标位置
            }
            finally
            {
                isUpdatingSource = false; // 关闭防死循环锁
            }
        }
        private void RowComboBox_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSource) return;
            ComboBox combo = sender as ComboBox;
            string inputText = combo.Text.Trim();

            if (int.TryParse(inputText, out int targetValue))
            {
                // 创建两个临时列表，单次遍历时直接分类
                List<int> smallerList = new List<int>();
                List<int> greaterList = new List<int>();

                // 【核心优化】只进行一次单向遍历
                foreach (int x in rowtList)
                {
                    if (x <= targetValue)
                    {
                        smallerList.Add(x);
                        // 性能技巧：如果已经存了超过100个，删掉最早的(最左边的)，确保只留最接近 targetValue 的100个
                        if (smallerList.Count > 100)
                        {
                            smallerList.RemoveAt(0);
                        }
                    }
                    else if (x > targetValue)
                    {
                        greaterList.Add(x);
                        // 性能技巧：因为 rowtList 通常是升序的，大于 targetValue 的数一旦攒够 100 个，可以直接终止整场循环！
                        if (greaterList.Count >= 100)
                        {
                            break;
                        }
                    }
                }

                // 单次循环结束后，直接将两个大小最多为100的集合合并
                var combinedList = smallerList.Concat(greaterList).ToList();

                // 安全更新数据源
                UpdateComboDataSource((ComboBox)sender, combinedList, inputText);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //防御代码
            if (mainForm._currentDocument == null) return;
            List<int> calList = new List<int>();
            if (int.TryParse(comboBox1.Text, out int startIndex) && int.TryParse(comboBox2.Text, out int endIndex))
            {
                for (int i = Math.Min(startIndex - 1, endIndex - 1); i <= Math.Max(startIndex - 1, endIndex - 1); i++)
                {
                    calList.Add(i);
                }
            }
            else
            {
                //抛出异常
                MessageBox.Show("开始行 或者 结束行 数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!(comboBox3.SelectedItem is DntFieldDescription operColField))
            {
                //抛出异常
                MessageBox.Show("操作列数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!(comboBox6.SelectedItem is DntFieldDescription referColField))
            {
                //抛出异常
                MessageBox.Show("参考列数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if ( !(comboBox4.SelectedValue is CompareOperator judgeCompare))
            {
                //抛出异常
                MessageBox.Show("判断规则数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!(comboBox7.SelectedValue is CompareOperator referCompare))
            {
                //抛出异常
                MessageBox.Show("参考规则数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!(comboBox5.SelectedValue is CalcOperator Operator))
            {
                //抛出异常
                MessageBox.Show("判断规则数据错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string judgeText = textBox1.Text.Trim();    //判断文本
            string calcText = textBox2.Text.Trim();     //计算文本
            string referText = textBox3.Text.Trim();    //参考文本

            //开始修改
            for (int i = 0; i < calList.Count; i++)
            {
                int realRowIndex = (int)mainForm.dList[calList[i]];
                object newValue;
                object oldvalue;
                //判断是否使用了参考和判断规则
                if (judge(checkBox1, realRowIndex, judgeText, operColField, judgeCompare) && judge(checkBox2, realRowIndex, referText, referColField, referCompare))
                {
                    var colList = mainForm._currentDocument.Columns[operColField.FieldName];
                    //按照运算符规则进行数据修改
                    switch (operColField.FieldType)
                    {
                        case DntFieldType.Text:
                            oldvalue = colList[realRowIndex];
                            newValue = calculator(Operator, oldvalue, calcText, colList[0], i);
                            if (newValue != null)
                            {
                                colList[realRowIndex] = (string)newValue;
                            }
                            break;
                        case DntFieldType.BooleanInt:
                        case DntFieldType.Int32:
                            oldvalue = colList[realRowIndex];
                            newValue = calculator(Operator, oldvalue, calcText, colList[0], i);
                            if (newValue != null)
                            {
                                colList[realRowIndex] = int.Parse(newValue.ToString());
                            }
                            break;
                        case DntFieldType.Percentage:
                        case DntFieldType.Float:
                            oldvalue = colList[realRowIndex];
                            newValue = calculator(Operator, oldvalue, calcText, colList[0], i);
                            if (newValue != null)
                            {
                                colList[realRowIndex] = float.Parse(newValue.ToString());
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            mainForm.dataGridView1.Refresh();
        }
        private object calculator(CalcOperator Operator,object value,object calcValue,object firstValue,int index)
        {
            double doubleValue, calcDoule,firstDouble;
            switch (Operator)
            {
                case CalcOperator.Equals:
                    return calcValue;
                case CalcOperator.Add:
                    if (double.TryParse(value.ToString(),out doubleValue) && double.TryParse(calcValue.ToString(), out calcDoule))
                    {
                        return doubleValue + calcDoule;
                    }
                    break;
                case CalcOperator.Subtract:
                    if (double.TryParse(value.ToString(), out doubleValue) && double.TryParse(calcValue.ToString(), out calcDoule))
                    {
                        return doubleValue - calcDoule;
                    }
                    break;
                case CalcOperator.Multiply:
                    if (double.TryParse(value.ToString(), out doubleValue) && double.TryParse(calcValue.ToString(), out calcDoule))
                    {
                        return doubleValue * calcDoule;
                    }
                    break;
                case CalcOperator.Divide:
                    if (double.TryParse(value.ToString(), out doubleValue) && double.TryParse(calcValue.ToString(), out calcDoule))
                    {
                        return doubleValue / calcDoule;
                    }
                    break;
                case CalcOperator.ArithmeticProgression:
                    if (double.TryParse(firstValue.ToString(), out firstDouble) && double.TryParse(calcValue.ToString(), out calcDoule))
                    {
                        return firstDouble + calcDoule * index;
                    }
                    break;
                default:
                    break;
            }
            return null;
        }
        private bool judge(CheckBox checkBox,int index,string judgeText, DntFieldDescription colField, CompareOperator judgeCompare)
        {
            //如果没有选择判断功能,直接返回true,进入修改方法
            if (!checkBox.Checked) return true;
            //已经选择判断功能,需要获取当前rowIndex的数据,根据判断运算逻辑,进行判断
            var colValue = mainForm._currentDocument.Columns[colField.FieldName][index];
            switch (judgeCompare)
            {
                case CompareOperator.Equals:
                    return colValue.ToString().Equals(judgeText);
                case CompareOperator.GreaterThan:
                    if (double.TryParse(judgeText,out double greater) && double.TryParse(colValue.ToString(), out double greaterColValue))
                    {
                        return greaterColValue > greater;
                    }
                    break;
                case CompareOperator.LessThan:
                    if (double.TryParse(judgeText, out double less) && double.TryParse(colValue.ToString(), out double lessColValue))
                    {
                        return lessColValue < less;
                    }
                    break;
                case CompareOperator.GreaterThanOrEqual:
                    if (double.TryParse(judgeText, out double greaterEqual) && double.TryParse(colValue.ToString(), out double greaterEqualColValue))
                    {
                        return greaterEqualColValue >= greaterEqual;
                    }
                    break;
                case CompareOperator.LessThanOrEqual:
                    if (double.TryParse(judgeText, out double lessEqual) && double.TryParse(colValue.ToString(), out double lessEqualColValue))
                    {
                        return lessEqualColValue <= lessEqual;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
    }
}
