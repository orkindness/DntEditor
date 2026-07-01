using DntEditor_Hang.Helpers;
using DntEditor_Hang.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class IniTranslationForm2 : Form
    {
        private MainForm mainForm;
        private string otherPath = GlobalHelper.AppRootPath + translationDicts.SourcePath + translationDicts.otherSourcePath;
        public IniTranslationForm2(MainForm mForm)
        {
            InitializeComponent();
            mainForm = mForm;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;
        }

        private void IniTranslationForm2_Load(object sender, EventArgs e)
        {
            if(this.Owner != null)
            {
                int left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                int top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;

                this.Location = new Point(left, top);
            }

            this.textBox1.Text = Path.GetFileNameWithoutExtension(mainForm._currentLoadedFilePath);
            //comboBox1.SelectedItem;
            initializetionCheckBox();
            LoadFilesToGrid(otherPath);

            if (mainForm.dataGridView1.CurrentCell != null)
            {
                this.label1.Text = "例：[" + mainForm.dataGridView1.Rows[mainForm.dataGridView1.CurrentCell.RowIndex].Cells[comboBox1.SelectedIndex].Value.ToString() + "=" + mainForm.dataGridView1.Rows[mainForm.dataGridView1.CurrentCell.RowIndex].Cells[0].Value + "]";
            }
            else
            {
                this.label1.Text = "例：";
            }
        }
        private void LoadFilesToGrid(string folderPath)
        {
            // 1. 基础安全校验
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }
            // 3. 获取目标路径下所有的 .dnt 文件
            string[] iniFiles = Directory.GetFiles(folderPath, "*.ini", SearchOption.TopDirectoryOnly);

            // 4. 开始匹配并组装数据集
            List<FileItem> itemList = new List<FileItem>();
            int serialNumber = 1;

            foreach (string filePath in iniFiles)
            {
                // 获取带后缀的文件名（例如: skill.dnt）
                string fileNameWithExt = Path.GetFileName(filePath);

                itemList.Add(new FileItem
                {
                    序列= serialNumber++,
                    文件名称 = fileNameWithExt
                });
            }

            // 5. 渲染绑定到 dataGridView2
            BindToDataGridView(itemList);
        }
        private void BindToDataGridView(List<FileItem> items)
        {
            // 清空旧数据和旧列头
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();

            // 开启双缓冲，防止大数量滚动时表格闪烁（硬核优化）
            Type dgvType = dataGridView1.GetType();
            System.Reflection.PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi?.SetValue(dataGridView1, true, null);

            // 绑定新数据源
            dataGridView1.DataSource = items;

            // 汉化并配置列名
            //dataGridView1.Columns[0].HeaderText = "序列";
            dataGridView1.Columns["序列"].Width = 50; // 序列列可以窄一点

            //dataGridView1.Columns[1].HeaderText = "文件名称";
            dataGridView1.Columns["文件名称"].Width = 200;


            // 让最后一列“中文名称”自动填满剩余的所有表格宽度，美化排版
            dataGridView1.Columns["文件名称"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // 禁止用户在表格最后一行手动添加空白新行
            dataGridView1.AllowUserToAddRows = false;
            // 设置为只能整行选择，不能单个单元格选择，更符合目录操作习惯
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ReadOnly = true;
        }
        private void initializetionCheckBox()
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

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("没有选择文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult result = MessageBox.Show(
                "确定要合并选择的文件,并保存文件成["+this.textBox1.Text+".ini]吗?", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );
            try
            {
                // 2. 根据用户的点击结果，执行不同的业务逻辑
                if (result == DialogResult.Yes)
                {
                    List<string> selects = new List<string>();
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    {
                        string fileName = row.Cells[1].Value?.ToString() ?? "";
                        string filePath = otherPath + fileName;
                        selects.Add(filePath);
                    }

                    string targetIniPath = Path.Combine(otherPath, $"{this.textBox1.Text}.ini");
                    GlobalHelper.MergeIniFiles(selects, targetIniPath);

                    MessageBox.Show("保存成功！文件保存在:" + targetIniPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (result == DialogResult.No)
                {
                    // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                    // 这里通常留空，或者执行后续不需要保存的刷新操作
                }
            }
            catch
            {
                MessageBox.Show("合并文件错误!!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadFilesToGrid(otherPath);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string baseTypeName = comboBox1.SelectedItem.ToString();
            int rowCount = mainForm.dataGridView1.Rows.Count;
            if (string.IsNullOrEmpty(baseTypeName) || rowCount <= 0)
            {
                return;
            }
            var mergedResultDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rowCount; i++)
            {
                int colIndex = comboBox1.SelectedIndex;
                string key = mainForm.dataGridView1.Rows[i].Cells[colIndex].Value.ToString();
                string sValue = mainForm.dataGridView1.Rows[i].Cells[0].Value.ToString();
                mergedResultDict[key] = sValue;
            }
            if (mergedResultDict.Count > 0)
            {
                string targetIniPath = Path.Combine(otherPath, $"{this.textBox1.Text}.ini");
                DntBatchTranslator.WriteMergedDictToIni(targetIniPath, mergedResultDict);

                MessageBox.Show("保存成功！文件保存在:" + targetIniPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            LoadFilesToGrid(otherPath);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("没有选择文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult result = MessageBox.Show(
                "确定要删除选择的文件吗?", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );
            try
            {
                // 2. 根据用户的点击结果，执行不同的业务逻辑
                if (result == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    {
                        string fileName = row.Cells[1].Value?.ToString() ?? "";
                        File.Delete(otherPath + fileName);
                    }
                    MessageBox.Show("删除成功!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (result == DialogResult.No)
                {
                    // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                    // 这里通常留空，或者执行后续不需要保存的刷新操作
                }
            }
            catch
            {
                MessageBox.Show("删除文件错误!!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadFilesToGrid(otherPath);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (mainForm.dataGridView1.CurrentCell != null)
            {
                this.label1.Text = "例：[" + mainForm.dataGridView1.Rows[mainForm.dataGridView1.CurrentCell.RowIndex].Cells[comboBox1.SelectedIndex].Value.ToString() + "=" + mainForm.dataGridView1.Rows[mainForm.dataGridView1.CurrentCell.RowIndex].Cells[0].Value + "]";
            }
            else
            {
                this.label1.Text = "例：";
            }
        }
    }
    public class FileItem
    {
        public int 序列 { get; set; }      // 属性名直接叫中文，绑定时连 HeaderText 都不用改了
        public string 文件名称 { get; set; }
    }
}
