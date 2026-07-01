using System;
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
    public partial class SearchForm : Form
    {
        private MainForm mainForm;
        public SearchForm(MainForm mForm)
        {
            InitializeComponent();
            mainForm = mForm;
            iniSearchForm();

            this.MaximizeBox = false;     // 隐藏/禁用最大化按钮
            this.MinimizeBox = false;     // 隐藏/禁用最小化按钮
            this.ControlBox = true;       // 确保关闭按钮可见
            
        }
        private void iniSearchForm()
        {
            // 1.创建一个列表用于存放列对象
            List<ColumnItem> columnItems = new List<ColumnItem>();

            // 2. 遍历 DataGridView 的所有列
            foreach (DataGridViewColumn col in mainForm.dataGridView1.Columns)
            {
                // 过滤：如果列是隐藏的，可以选择不放进 ComboBox 里（视你的业务而定）
                if (!col.Visible) continue;

                columnItems.Add(new ColumnItem
                {
                    HeaderText = col.HeaderText, // 比如 "列1"、"主键ID"
                    ColumnName = col.Name        // 比如 "Column1"、"PKID"
                });
            }

            // 3. 先解绑数据源，防止冲突
            comboBox1.DataSource = null;
            comboBox1.Items.Clear();

            // 4. 绑定提取出的列对象组
            comboBox1.DataSource = columnItems;
            comboBox1.DisplayMember = "HeaderText"; // 让用户在下拉框里看到易懂的表头文字
            comboBox1.ValueMember = "ColumnName";   // 后台隐藏实际的列名，方便后续写代码

            // 5. 切换选中item
            // 1. 安全检查：确保当前有处于激活状态的单元格
            if (mainForm.dataGridView1.CurrentCell != null)
            {
                // 2. 获取当前选中单元格所在列的后台 Name (例如 "PKID" 或 "Column1")
                string currentColumnName = mainForm.dataGridView1.CurrentCell.OwningColumn.Name;

                // 3. 让 ComboBox 自动选中对应的项
                // 因为 ComboBox 的 ValueMember 绑定的就是 ColumnName，所以直接给 SelectedValue 赋值即可
                comboBox1.SelectedValue = currentColumnName;
            }

            //6. 赋予查询文本框单元格内容
            if (mainForm.dataGridView1.CurrentCell!=null)
            {
                this.textBox1.Text = mainForm.dataGridView1.CurrentCell.Value.ToString();
            }
            else
            {
                this.textBox1.Text = "";
            }
           
        }
        private void SearchForm_Load(object sender, EventArgs e)
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
        /// 查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string targetColumnName = comboBox1.SelectedValue.ToString();
        }
    }
    public class ColumnItem
    {
        public string HeaderText { get; set; } // 界面上显示给用户看的列名（如：唯一标识）
        public string ColumnName { get; set; } // 代码里实际调用的列名（如：PKID）
    }
}
