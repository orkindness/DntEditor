using DntEditor_Hang.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class FilterForm : Form
    {
        private List<uint> dList = null;
        private DntDocument document = null;

        private Dictionary<string, List<DataItem>> dataDics = new Dictionary<string, List<DataItem>>();
        private List<string> colNameList = null;

        private List<FilterParamCombo> paramList = null;
        private bool isUpdatingItems;

        private int checkListCount = 40000;
        public FilterForm(List<uint> list, DntDocument doc)
        {
            InitializeComponent();
            this.MaximizeBox = false;
            dList = list;
            document = doc;
            InitializeFilterParamCombo();
        }

        private void InitializeFilterParamCombo()
        {
            //获取列名List
            if (document != null)
            {
                colNameList = new List<string>();
                foreach (var name in document.Columns.Keys)
                {
                    colNameList.Add(name);
                }
            }

            checkListCount = int.Parse(this.textBox1.Text);

            paramList = new List<FilterParamCombo>();
            paramList.Add(new FilterParamCombo());
            PanelAddFilter(0);
            loadDataDics(1, 0);
        }
        /// <summary>
        /// 重置功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            //清空容器控件
            this.panel1.Controls.Clear();
            //List重新赋值
            InitializeFilterParamCombo();
        }
        /// <summary>
        /// add增加条件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            if (paramList == null) return;
            paramList.Add(new FilterParamCombo());
            PanelAddFilter(paramList.Count - 1);
        }
        private void PanelAddFilter(int index)
        {
            if (index < 0 || index >= paramList.Count) return;
            int dirtY = 84 - 12;
            FilterParamCombo combo = paramList[index];
            if (index != 0)
            {
                //this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 12);
                //this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 84);
                int x = combo.ParamOperatorComboBox.Location.X;
                int y = combo.ParamOperatorComboBox.Location.Y;
                combo.ParamOperatorComboBox.Location = new System.Drawing.Point(x, y + dirtY * index);
                this.panel1.Controls.Add(combo.ParamOperatorComboBox);

                combo.cancelBt.Click += new System.EventHandler(this.cancel_Click);
            }
            else
            {   //令初始行取消按钮失效
                combo.cancelBt.Enabled = false;
            }
            //为运算符添加方法;
            combo.OperatorComboBox.SelectedValueChanged += operator_SelectedValueChanged;
            combo.OperatorComboBox.Tag = index;

            if (colNameList != null)
            {
                //为列combobox控件赋值
                combo.ColumnComboBox.Items.AddRange(colNameList.ToArray());
                combo.ColumnComboBox.SelectedIndex = 1;//让选择框默认选中"PKID"
                combo.ColumnComboBox.SelectedIndexChanged += this.colCombobox_SelectedIndexChanged;
                combo.ColumnComboBox.Tag = index;

            }
            //为取消按钮赋值,添加方法;
            combo.cancelBt.Tag = index;
            //为等于的textbox.tag赋值
            combo.equlTextBox.Tag = index;
            //combo.equlTextBox.Enter += new System.EventHandler(this.EqualTextBox_Enter);
            combo.equlTextBox.TextChanged += new System.EventHandler(this.EqualTextBox_changed);

            //输入框初始显示:
            combo.equlTextBox.Visible = true;
            combo.textBox.Visible = false;
            combo.MaxTextBox.Visible = false;

            combo.flowLayoutPanel.Location = new System.Drawing.Point(combo.flowLayoutPanel.Location.X, combo.flowLayoutPanel.Location.Y + dirtY * index);
            this.panel1.Controls.Add(combo.flowLayoutPanel);

        }


        private void colCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (document == null) return;
            ComboBox box = sender as ComboBox;
            if (box.Tag is int index)
            {

                if (box.SelectedIndex == 0)
                {
                    paramList[index].OperatorComboBox.Items.Clear();
                    paramList[index].OperatorComboBox.Items.AddRange(paramList[index].operatorList2);
                    paramList[index].OperatorComboBox.SelectedIndex = 0;
                }
                else if (box.SelectedIndex == 1)
                {
                    paramList[index].OperatorComboBox.Items.Clear();
                    paramList[index].OperatorComboBox.Items.AddRange(paramList[index].operatorList1);
                    paramList[index].OperatorComboBox.SelectedIndex = 0;
                }
                else
                {
                    DntFieldType type = document.Fields[box.SelectedIndex - 2].FieldType;
                    switch (type)
                    {
                        case DntFieldType.Text:
                            paramList[index].OperatorComboBox.Items.Clear();
                            paramList[index].OperatorComboBox.Items.AddRange(paramList[index].operatorList2);
                            paramList[index].OperatorComboBox.SelectedIndex = 0;
                            break;
                        case DntFieldType.BooleanInt:
                        case DntFieldType.Int32:
                        case DntFieldType.Percentage:
                        case DntFieldType.Float:
                        default:
                            paramList[index].OperatorComboBox.Items.Clear();
                            paramList[index].OperatorComboBox.Items.AddRange(paramList[index].operatorList1);
                            paramList[index].OperatorComboBox.SelectedIndex = 0;
                            break;
                    }
                }
            }

        }

        private void operator_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            int sIndex = box.SelectedIndex;
            string value = box.Items[sIndex]?.ToString() ?? "";
            if (document == null) return;
            if (box.Tag is int index)
            {
                FilterParamCombo paraCombo = paramList[index];
                switch (value)
                {
                    case "=":
                    case "等于":
                        paraCombo.equlTextBox.Visible = true;
                        paraCombo.textBox.Visible = false;
                        paraCombo.MaxTextBox.Visible = false;
                        //添加一个方法:加载combobox的item,.初始化当前表格数据的distinctList;
                        loadDataDics(paraCombo.ColumnComboBox.SelectedIndex,index);
                        break;
                    case "between":
                        paraCombo.equlTextBox.Visible = false;
                        paraCombo.textBox.Visible = true;
                        paraCombo.MaxTextBox.Visible = true;
                        this.checkedListBox1.Items.Clear();
                        break;
                    default:
                        paraCombo.equlTextBox.Visible = false;
                        paraCombo.textBox.Visible = true;
                        paraCombo.MaxTextBox.Visible = false;
                        this.checkedListBox1.Items.Clear();
                        break;
                }
            }
        }

        private void loadDataDics(int colIndex,int paramIndex)
        {
            if (colIndex == null || colNameList == null || document ==null) return;
            string colName = colNameList[colIndex];
            
            if (dataDics.ContainsKey(colName))
            {
                loadCheckList(paramIndex,checkListCount);
                return;
            }

            if (document.Columns.ContainsKey(colName))
            {
                List<DataItem> itemList = new List<DataItem>();
                int rowIndex = 0;

                List<object> colValueList = document.Columns[colName].Cast<object>().Distinct().ToList();

                DataItem item_first = new DataItem();
                item_first.Index = -1;
                item_first.IsChecked = false;
                item_first.Name = "全选";
                itemList.Add(item_first);

                foreach (var colValue in colValueList)
                {
                    DataItem item = new DataItem();
                    item.Index = rowIndex++;
                    item.IsChecked = false;
                    item.Name = colValue.ToString();
                    itemList.Add(item);
                }
                dataDics[colName] = itemList;
                loadCheckList(paramIndex, checkListCount);
            }
           
        }
        private void loadCheckList(int paramIndex,int count)
        {
            // 打开状态锁，告诉 ItemCheck 事件这是程序在刷新 UI，不是用户在手动勾选
            isUpdatingItems = true;

            FilterParamCombo filterCombo = paramList[paramIndex];
            //当前列名
            string colName = colNameList[filterCombo.ColumnComboBox.SelectedIndex];
            string textString = filterCombo.equlTextBox.Text.Trim();
            //CheckedListBox checkedList = filterCombo.checkedListBox;
            CheckedListBox checkedList = this.checkedListBox1;
            List <DataItem> itemList = dataDics[colName].Where(item => item.Name.Contains(textString)).ToList();
            //判断List和设置的List数据大小
            count = Math.Min(count, itemList.Count);
            // 1. 挂起绘图，防止大批量添加数据时界面闪烁、卡顿
            //checkedList.BeginUpdate();

            checkedList.Items.Clear();
            for (int i = 0; i < count; i++)
            {
                checkedList.Items.Add(itemList[i], itemList[i].IsChecked);
            }
            // 显示属性：让 CheckedListBox 界面显示 Name 属性，但内部依然持有整个 DataItem 对象
            checkedList.DisplayMember = "Name";
            checkedList.Tag = paramList;    //为后续itemcheked方法做准备,定位真实数据列
            // 【关键修正】：传输完数据后，强行给控件洗个澡，逼它重新布局
            //checkedList.PerformLayout(); // 强制控件内部重新计算布局
            //checkedList.Invalidate();     // 强行令控件重绘（刷新滚动条）

            // 4. 恢复绘图
            //checkedList.EndUpdate();
            // 关闭状态锁
            isUpdatingItems = false;
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //状态锁,预防loadCheckList()方法被误判为点击事件
            if (isUpdatingItems) return;

            DataItem item = (DataItem)checkedListBox1.Items[e.Index];
        }
        private void PanelRemoveFilter(int index)
        {
            if (index < 0 || index >= paramList.Count) return;
            int dirtY = 84 - 12;
            FilterParamCombo combo = paramList[index];
            if (index != 0)
            {
                this.panel1.Controls.Remove(combo.flowLayoutPanel);
                this.panel1.Controls.Remove(combo.ParamOperatorComboBox);
            }
            else return;
            for (int i = index+1; i < paramList.Count; i++)
            {
                FilterParamCombo comboAfter = paramList[i];
                //为取消按钮赋值,添加方法;
                comboAfter.cancelBt.Tag = i-1;

                comboAfter.ParamOperatorComboBox.Location = new System.Drawing.Point(comboAfter.ParamOperatorComboBox.Location.X, comboAfter.ParamOperatorComboBox.Location.Y - dirtY );

                comboAfter.flowLayoutPanel.Location = new System.Drawing.Point(comboAfter.flowLayoutPanel.Location.X, comboAfter.flowLayoutPanel.Location.Y - dirtY);
            }
            paramList.Remove(combo);


        }
        private void FilterForm_Load(object sender, EventArgs e)
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
        private void cancel_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            if (btn.Tag is int index)
            {
                PanelRemoveFilter(index);
            }
            else return;
        }
        //等于列表的textbox方法,后补
        private void textBox5_MouseClick(object sender, MouseEventArgs e)
        {
            // 1. 将 sender 转换为 TextBox 控件
            TextBox clickedTextBox = sender as TextBox;
            // 鼠标点击时，在 TextBox 下方弹出 CheckListbox
            //dropDown.Width = clickedTextBox.Width;
            //dropDown.Show(clickedTextBox, new Point(0, clickedTextBox.Height), ToolStripDropDownDirection.BelowRight);
        }

        private void EqualTextBox_Enter(object sender, EventArgs e)
        {
            // 1. 将 sender 转换为 TextBox 控件
            TextBox ETextBox = sender as TextBox;
            //获取索引
            if (ETextBox.Tag is int index)
            {
                //FilterParamCombo combo = paramList[index];

                //combo.dropDown.Width = 2 * ETextBox.Width;
                //combo.dropDown.Show(ETextBox, new Point(0, ETextBox.Height), ToolStripDropDownDirection.BelowRight);
            }
        }
        private void EqualTextBox_changed(object sender, EventArgs e)
        {
            // 1. 每当文本改变，立刻停止之前的倒计时
            timer1.Stop();

            timer1.Tag = ((TextBox)sender).Tag;
            // 2. 重新开始倒计时 500 毫秒
            timer1.Start();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            Timer tm = sender as Timer;
            if (tm.Tag is int paramIndex)
            {
                int colIndex = paramList[paramIndex].ColumnComboBox.SelectedIndex;
                loadDataDics(colIndex, paramIndex);

            }
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // e.KeyChar != 8 代表允许用户使用 Backspace（退格键）删除数字
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != 8)
            {
                // 设置为 true 代表该按键已被处理（即拦截掉，不显示在文本框中）
                e.Handled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || string.IsNullOrEmpty(tb.Text)) return;

            // 使用正则表达式，只匹配纯数字（0-9）
            // 如果你允许输入负整数，可以将正则改为: @"^-?\d*$"
            if (!System.Text.RegularExpressions.Regex.IsMatch(tb.Text, @"^\d*$"))
            {
                // 发现非法字符，直接清空或者恢复为上一次的值
                // 这里采用最直接的方式：只保留文本中的纯数字
                string cleanString = System.Text.RegularExpressions.Regex.Replace(tb.Text, @"[^\d]", "");

                // 记录当前光标位置，防止重写文本后光标跳到最前面
                int selectionStart = tb.SelectionStart;

                tb.Text = cleanString;

                // 恢复光标位置
                tb.SelectionStart = Math.Min(selectionStart, tb.Text.Length);
            }
            checkListCount = int.Parse(tb.Text.Trim());
        }

    }
    // 数据实体类
    public class DataItem
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
