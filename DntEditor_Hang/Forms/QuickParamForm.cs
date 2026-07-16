using DntEditor_Hang.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class QuickParamForm : Form
    {
        private string currentFilePath = "";
        private MainForm mainForm; // 保存主窗体的引用

        // 构造函数：接收主窗体的引用
        public QuickParamForm(MainForm mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.Text = "快捷参数窗口";

            // 初始化布局与控件
            InitControls();
        }

        private void InitControls()
        {
            // 1. 初始化菜单栏
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem menuSelectFile = new ToolStripMenuItem("选择文件");
            menuSelectFile.Click += MenuSelectFile_Click;
            menuStrip.Items.Add(menuSelectFile);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // 2. 初始化 DataGridView
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.AllowUserToAddRows = false; // 禁止用户手动添加行
            dataGridView1.RowHeadersVisible = false;  // 隐藏行头
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // 1. 禁止用户通过鼠标拖拽改变所有行的高度
            dataGridView1.AllowUserToResizeRows = false;

            // 3. 创建 4 列
            dataGridView1.Columns.Add("ParamName", "参数");
            dataGridView1.Columns.Add("ParamDesc", "参数说明");

            // 功能1：复制按钮列
            DataGridViewButtonColumn btnCopyCol = new DataGridViewButtonColumn();
            btnCopyCol.Name = "BtnCopy";
            btnCopyCol.HeaderText = "功能1";
            btnCopyCol.Text = "复制";
            btnCopyCol.UseColumnTextForButtonValue = true; // 让按钮显示文字
            dataGridView1.Columns.Add(btnCopyCol);

            // 功能2：插入按钮列
            DataGridViewButtonColumn btnInsertCol = new DataGridViewButtonColumn();
            btnInsertCol.Name = "BtnInsert";
            btnInsertCol.HeaderText = "功能2";
            btnInsertCol.Text = "插入";
            btnInsertCol.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(btnInsertCol);

            // 如果使用自动填充，你还可以精细控制每一列的比例或固定某些列：
            dataGridView1.Columns["ParamName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridView1.Columns["ParamName"].Width = 60;         // 固定宽度 70 像素
            dataGridView1.Columns["ParamDesc"].FillWeight = 40;  // “参数说明”列占 40% 剩余空间

            // 按钮列不需要太宽，可以设置固定宽度（设置 Width 前需要把该列的 AutoSizeMode 设为 None）
            dataGridView1.Columns["BtnCopy"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridView1.Columns["BtnCopy"].Width = 70;         // 固定宽度 70 像素

            dataGridView1.Columns["BtnInsert"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridView1.Columns["BtnInsert"].Width = 70;       // 固定宽度 70 像素
            // 绑定单元格点击事件
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;

            // 4. 窗体加载时默认读取 .\Param\快捷参数.ini
            this.Load += QuickParamForm_Load;
        }

        private void QuickParamForm_Load(object sender, EventArgs e)
        {
            // 获取默认路径 .\Param\快捷参数.ini
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Param", "快捷参数.ini");
            LoadIniData(defaultPath);

            // 检查是否有所有者（即主窗体）
            if (this.Owner != null)
            {
                // 核心公式：子窗口坐标 = 主窗口坐标 + (主窗口宽高 - 子窗口宽高) / 2
                int left = this.Owner.Left + (this.Owner.Width);
                int top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;

                this.Location = new Point(left, top);
            }
        }

        // 读取并解析 INI 文件到 DataGridView
        private void LoadIniData(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"未找到配置文件：{filePath}\n请通过菜单栏选择文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            currentFilePath = filePath;
            dataGridView1.Rows.Clear(); // 清空旧数据

            try
            {
                // 按行读取文件，支持大文件且编码自动识别
                var lines = File.ReadLines(filePath, System.Text.Encoding.UTF8);

                foreach (string line in lines)
                {
                    // 去除首尾空格
                    string trimmedLine = line.Trim();

                    // 跳过空行、注释行(分号或井号开头)以及可能存在的方括号节(如 [Setting])
                    if (string.IsNullOrEmpty(trimmedLine) ||
                        trimmedLine.StartsWith(";") ||
                        trimmedLine.StartsWith("#") ||
                        (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]")))
                    {
                        continue;
                    }

                    // 寻找第一个等号的位置
                    int equalSignIndex = trimmedLine.IndexOf('=');
                    if (equalSignIndex > 0)
                    {
                        // 等号前面是 key (参数)
                        string param = trimmedLine.Substring(0, equalSignIndex).Trim();
                        // 等号后面是 value (参数说明)
                        string desc = trimmedLine.Substring(equalSignIndex + 1).Trim();

                        // 将解析出的数据添加至表格（4列中的前2列，后2列按钮会自动生成）
                        dataGridView1.Rows.Add(param, desc);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析INI文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 菜单栏点击：选择文件
        private void MenuSelectFile_Click(object sender, EventArgs e)
        {
            // 默认打开 .\Param 文件夹
            string paramDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Param");
            if (Directory.Exists(paramDir))
            {
                Directory.CreateDirectory(paramDir);
            }
            string filePath = GlobalHelper.SelectFile("选择ini文件", "ini文件 (*.ini)", "*.ini", paramDir);
            if (!string.IsNullOrEmpty(filePath))
            {
                // 严谨校验：必须是 .dnt 后缀
                if (Path.GetExtension(filePath).ToLower() == ".ini")
                {
                    LoadIniData(filePath);
                }
                else
                {
                    MessageBox.Show(this, "仅支持读取 .ini 格式的二进制文件！", "错误");
                }
            }
            /***
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                // 默认打开 .\Param 文件夹
                string paramDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Param");
                if (Directory.Exists(paramDir))
                {
                    ofd.InitialDirectory = paramDir;
                }

                ofd.Filter = "INI 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";
                ofd.Title = "选择快捷参数文件";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadIniData(ofd.FileName);
                }
            }***/
        }

        // 处理 DataGridView 中的按钮点击事件
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // 排除列头点击
            if (e.RowIndex < 0) return;

            // 获取当前行的“参数”列数据
            string paramValue = dataGridView1.Rows[e.RowIndex].Cells["ParamName"].Value?.ToString() ?? "";

            // 点击了【复制】按钮
            if (dataGridView1.Columns[e.ColumnIndex].Name == "BtnCopy")
            {
                if (!string.IsNullOrEmpty(paramValue))
                {
                    Clipboard.SetText(paramValue); // 写入剪贴板
                    MessageBox.Show($"已复制到剪贴板: {paramValue}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            // 点击了【插入】按钮
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "BtnInsert")
            {
                if (mainForm != null)
                {
                    //MessageBox.Show($"已插入: {paramValue}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // 调用主窗体的方法，将数据写入主窗体的 DataGridView
                    mainForm.InsertValueToSelectedCell(paramValue);
                }
            }
        }
    }
}