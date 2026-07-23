using DntEditor_Hang.Models;
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
    public partial class AddColumnForm : Form
    {
        MainForm mainForm = null;
        public AddColumnForm(MainForm mForm)
        {
            InitializeComponent();
            mainForm = mForm;
            combobox_Load();
        }
        private void combobox_Load()
        {
            // 1. 将枚举的名称和值转换为一个列表
            var dataSource = Enum.GetValues(typeof(DntFieldType))
                                 .Cast<DntFieldType>()
                                 .Select(x => new
                                 {
                                     Name = x.ToString(),     // 界面显示的文本，例如 "Text"
                                 Value = (byte)x          // 后台实际代表的 byte 值，例如 1
                             }).ToList();

            // 2. 绑定到 ComboBox
            comboBox1.DataSource = dataSource;
            comboBox1.DisplayMember = "Name"; // 告诉控件显示 Name 属性
            comboBox1.ValueMember = "Value";   // 告诉控件后台值使用 Value 属性
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var _currentDocument = mainForm._currentDocument;
            var dataGridView1 = mainForm.dataGridView1;

            string colName = textBox1.Text.Trim();
            DntFieldType colType = (DntFieldType)comboBox1.SelectedValue;
            if (string.IsNullOrEmpty(colName))
            {
                MessageBox.Show("请输入列名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 2. 查重：判断 Fields 列表中是否已经存在相同的 FieldName
            // 【注】这里使用了 StringComparison.OrdinalIgnoreCase，表示“不区分大小写”（如 ID 和 id 算重复）
            // 如果你严格区分大小写，可以换成 == 或 StringComparison.Ordinal
            bool isDuplicate = _currentDocument.Fields.Any(f => f.FieldName.Equals(colName, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                // 发现重复，直接返回 
                MessageBox.Show("列名重复,添加失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DntFieldDescription newFiled = new DntFieldDescription
            {
                FieldName = colName,
                FieldType = colType
            };


            if (_currentDocument == null) return;
            int colIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            if (colIndex < 1)
            {
                MessageBox.Show("无法在第一列添加新列", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 1. 弹出询问对话框，设置标题、按钮（YesNoCancel）和警告图标
            DialogResult result = MessageBox.Show(
                $"是否添加列({newFiled.FieldName})？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {
                // 【步骤 1】停止渲染
                dataGridView1.SuspendLayout();

                int RecordCount = _currentDocument.Columns["PKID"].Count;
                System.Collections.IList columnList;

                switch (newFiled.FieldType)
                {
                    case DntFieldType.Text:
                        columnList = Enumerable.Repeat(string.Empty, RecordCount).ToList();
                        break;
                    case DntFieldType.BooleanInt:
                        columnList = Enumerable.Repeat(0, RecordCount).ToList();
                        break;
                    case DntFieldType.Int32:
                        columnList = Enumerable.Repeat(0, RecordCount).ToList();
                        break;
                    case DntFieldType.Percentage:
                    case DntFieldType.Float:
                        columnList = Enumerable.Repeat(0f, RecordCount).ToList();
                        break;
                    default:
                        columnList = Enumerable.Repeat(string.Empty, RecordCount).ToList();
                        break;
                }
                _currentDocument.Columns.Add(newFiled.FieldName, columnList);
                _currentDocument.Fields.Insert(colIndex - 2 + 1, newFiled);
                _currentDocument.FieldCount = (ushort)_currentDocument.Fields.Count;

                //为翻译列-覆盖List创建新列
                mainForm.colTranDict.Add(newFiled.FieldName,new ColumTranslationItem());
                mainForm.InitVirtualDataGridView(_currentDocument);

                // 2. 恢复控件的布局逻辑
                dataGridView1.ResumeLayout(true);
                //重新恢复焦点
                dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[colIndex];
                dataGridView1.Invalidate();

                statusLabel.columnCount = _currentDocument.FieldCount + 1;
                mainForm.statusStrip1.Text = statusLabel.mainStatusLabel();
                MessageBox.Show($"添加列({newFiled.FieldName}成功" , "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddColumnForm_Load(object sender, EventArgs e)
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
    }
    
}
