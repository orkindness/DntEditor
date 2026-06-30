using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DntEditor_Hang.Models;
using DntEditor_Hang.Helpers;
using System.IO;
using System.Diagnostics;

namespace DntEditor_Hang.Forms
{
    public partial class MainForm : Form
    {
        private SearchForm searchForm = null;
        private SetingForm setingForm = null;
        private BatchCryptoForm batchCryptoForm = null;
        private IniTranslationForm1 IniTranslationForm1 = null;
        private IniTranslationForm2 IniTranslationForm2 = null;
        private DntDeepSearchForm dntDeepSearchForm = null;
        private PakPackerForm PakPackerForm = null;

        private string _currentLoadedFilePath = string.Empty; // 记录当前打开的文件路径
        private string _currentLoadedFileName = string.Empty;// 记录当前打开的文件名
        private DntDocument _currentDocument = null;          // 记录当前打开的文档对象
        public MainForm()
        {
            InitializeComponent();
            AppConfig.Load();
            LoadDntFilesToGrid(AppConfig.DntPath);
            this.checkBox1.Checked = AppConfig.IsSyncSaveEnabled;//配置文件内容:同时保存明文密文目录


            this.DragDrop += MainForm_DragDrop;
            this.DragEnter += MainForm_DragEnter;

            this.panel4.DoubleClick += panel4_DoubleClick;

            this.dataGridView2.CellDoubleClick += dataGridView2_CellDoubleClick;

            this.dataGridView1.CellValueNeeded += dataGridView1_CellValueNeeded;
            this.dataGridView1.CellValuePushed += dataGridView1_CellValuePushed;
            this.dataGridView1.CellFormatting += dataGridView1_CellFormatting;
            this.dataGridView1.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            this.dataGridView1.CellClick += dataGridView1_CellClick;

            this.textBox1.KeyPress += textBox1_KeyPress;
            this.textBox1.TextChanged += textBox1_TextChanged;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            AppConfig.IsSyncSaveEnabled = checkBox1.Checked;
            AppConfig.Save();
        }
        public void checkBox1_CheckedChanged()
        {
            checkBox1.Checked = AppConfig.IsSyncSaveEnabled;
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            statusLabel.rowIndex = e.RowIndex+1;
            statusLabel.columIndex = e.ColumnIndex;
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();
        }

        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 【关键防御】排除点击列头（RowIndex = -1）或行头（ColumnIndex = -1）的情况
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // 获取当前双击的单元格对象
                DataGridViewCell clickedCell = dataGridView2.Rows[e.RowIndex].Cells[1];

                // 示例：获取当前单元格的值
                string cellValue = clickedCell.Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(cellValue))
                {
                    return;
                }
                string dntFilePath = AppConfig.DntPath + "\\" + cellValue;

                //MessageBox.Show($"您双击了第 {e.RowIndex + 1} 行，第 {e.ColumnIndex + 1} 列，内容为：{cellValue}", "提示");

                // 这里可以编写你的业务逻辑，比如双击某行弹出之前写好的修改/保存提示

                // 严谨校验：必须是 .dnt 后缀
                if (Path.GetExtension(dntFilePath).ToLower() == ".dnt")
                {
                    ExecuteLoadDntFile(dntFilePath);
                }

            }
        }


        #region 读取保存功能
        /// <summary>
        /// 通用的文件路径读取调用函数
        /// </summary>
        private void ExecuteLoadDntFile(string filePath)
        {
            // 1. 初始化并启动秒表
            statusLabel.stopwatch = new Stopwatch();
            statusLabel.stopwatch.Start();
            try
            {
                _currentLoadedFilePath = filePath;
                _currentLoadedFileName = Path.GetFileName(filePath);

                // 核心读取
                _currentDocument = DntConvertHelpers.LoadFromFile(filePath);

                // 2. 扔给虚拟化初始化方法，界面瞬间加载完毕！
                InitVirtualDataGridView(_currentDocument);
                UpdateRowHeaderWidth();

                statusLabel.rowsCount = (int)_currentDocument.RecordCount+1;
                statusLabel.columnCount = _currentDocument.FieldCount + 1;
                statusLabel.rowIndex = 1;
                statusLabel.columIndex = 0;

                this.dataGridView1.Visible = true;
                this.label2.Visible = false;
                this.label3.Visible = false;
                this.Text = $"DntEditor_Hang - [{Path.GetFileName(filePath)}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"读取DNT文件发生错误: {ex.Message}", "读取失败");
            }
            finally
            {
                // 3. 无论成功或失败，停止秒表
                statusLabel.stopwatch.Stop();
            }
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();
        }
        #endregion
        #region datagridview1虚拟化
        /// <summary>
        /// 初始化并开启虚拟模式表格
        /// </summary>
        private void InitVirtualDataGridView(DntDocument doc)
        {
            _currentDocument = doc;

            // 1. 核心：清空旧列，开启虚拟模式
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
            // 1. 必须先将模式改为 EnableResizing，否则无法修改高度
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;

            // 2. 设置表头高度为 40 像素
            dataGridView1.ColumnHeadersHeight = 35;
            dataGridView1.VirtualMode = true; // 【必须在代码或设计器中开启此属性】

            // 开启双缓冲防闪烁（.NET 4.7.2 经典硬核优化）
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(dataGridView1, true, null);

            // 2. 手动创建固定第一列：PKID
            DataGridViewTextBoxCell textCell = new DataGridViewTextBoxCell();
            // 1. 【核心调整】首先创建第 0 列：虚拟中文翻译列
            DataGridViewColumn transColumn = new DataGridViewColumn(textCell)
            {
                Name = "ChineseTranslation",
                HeaderText = "中文翻译",
                Width = 130,
                ReadOnly = true // 翻译列由 ini 配置决定，设为只读
            };
            dataGridView1.Columns.Add(transColumn);
            DataGridViewColumn pkidColumn = new DataGridViewColumn(textCell)
            {
                Name = "PKID",
                HeaderText = "PKID",
                Width = 120,
                //ReadOnly = true // 游戏主键通常不允许修改
            };
            dataGridView1.Columns.Add(pkidColumn);

            // 3. 根据DNT文件的描述块，动态创建后面的数据列
            foreach (var field in doc.Fields)
            {
                DataGridViewColumn dataColumn = new DataGridViewColumn(textCell)
                {
                    Name = field.FieldName,
                    HeaderText = field.FieldName,
                    Width = 120,
                    // 核心修改：禁用点击表头自动排序，允许我们自定义点击事件
                    SortMode = DataGridViewColumnSortMode.NotSortable
                    //AutoSizeMode = (DataGridViewAutoSizeColumnMode)DataGridViewAutoSizeColumnsMode.ColumnHeader
                };
                dataGridView1.Columns.Add(dataColumn);
            }

            // 4. 核心：告诉壳子表格一共有多少行数据，表格会自动生成垂直滚动条
            var pkidList = doc.Columns["PKID"] as List<uint>;
            dataGridView1.RowCount = pkidList.Count;
        }
        // 当表格需要渲染某个格子时，会自动触发这个事件
        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            // 核心更改 1：行数的防御性校验。总行数现在直接读取 "PKID" 这一列的长度
            if (_currentDocument == null || e.RowIndex < 0) return;

            var pkidList = _currentDocument.Columns["PKID"] as List<uint>;
            if (pkidList == null || e.RowIndex >= pkidList.Count) return;

            // 情况 A：第 0 列 —— 最左侧中文翻译列
            if (e.ColumnIndex == 0)
            {
                /***
                string currentDntFileName = Path.GetFileName(_currentLoadedFilePath);
                if (_translationDict != null && _translationDict.TryGetValue(currentDntFileName, out string chineseName))
                {
                    e.Value = chineseName;
                }
                else
                {
                    e.Value = "未关联翻译";
                }
                ***/
            }
            // 情况 B：第 1 列 —— PKID 主键列
            else if (e.ColumnIndex == 1)
            {
                // 核心更改 2：直接去分布式字典里的 "PKID" 强类型列中取数
                e.Value = pkidList[e.RowIndex];
            }
            // 情况 C：第 2 列及往后 —— DNT 常规数据列
            else
            {
                // 核心更改 3：传入 e.ColumnIndex - 1，从而在 GetFieldAt 内部正确跳过最前面的虚拟翻译列
                var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex - 1);

                if (fieldInfo != null && _currentDocument.Columns.ContainsKey(fieldInfo.FieldName))
                {
                    // 列式取数：找到该列对应的 IList 列表，拿出它的第 RowIndex 行数据
                    e.Value = _currentDocument.Columns[fieldInfo.FieldName][e.RowIndex];
                }
            }
        }
        // 当用户在表格格子里输入了新内容并敲回车/切换行时，自动触发此事件更新内存
        private void dataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            // 核心更改 1：行数校验。同样使用 "PKID" 列的总行数作为基准
            if (_currentDocument == null || e.RowIndex < 0) return;

            var pkidList = _currentDocument.Columns["PKID"] as List<uint>;
            if (pkidList == null || e.RowIndex >= pkidList.Count) return;

            // 核心更改 2：只有第 2 列及往后的数据列才允许修改（第0列翻译只读）
            if (e.ColumnIndex > 0)
            {

                string userInput = e.Value?.ToString() ?? string.Empty;
                if (e.ColumnIndex == 1)
                {
                    pkidList[e.RowIndex] = uint.Parse(userInput);
                }
                // 传入 e.ColumnIndex - 1，在 GetFieldAt 内部正确跳过最前方的虚拟翻译列，从而拿到 DNT 列定义
                var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex - 1);
                if (fieldInfo == null) return;

                // 核心更改 3：通过字段名称直接从分布式 Columns 字典中提取对应的列列表
                var columnList = _currentDocument.Columns[fieldInfo.FieldName];

                try
                {
                    // 核心更改 4：在 .NET 4.7.2 下，必须先将 columnList 转换为对应的强类型 List<T> 才能通过索引赋值
                    switch (fieldInfo.FieldType)
                    {
                        case DntFieldType.Text:
                            var listText = columnList as List<string>;
                            if (listText != null) listText[e.RowIndex] = userInput;
                            break;

                        case DntFieldType.BooleanInt:
                            var listBool = columnList as List<bool>;
                            if (listBool != null)
                            {
                                // 兼容输入 true/false 或 1/0
                                if (bool.TryParse(userInput, out bool bResult))
                                    listBool[e.RowIndex] = bResult;
                                else
                                    listBool[e.RowIndex] = (userInput == "1");
                            }
                            break;

                        case DntFieldType.Int32:
                            var listInt = columnList as List<int>;
                            if (listInt != null) listInt[e.RowIndex] = int.Parse(userInput);
                            break;

                        case DntFieldType.Percentage:
                        case DntFieldType.Float:
                            var listFloat = columnList as List<float>;
                            if (listFloat != null) listFloat[e.RowIndex] = float.Parse(userInput);
                            break;
                    }
                }
                catch
                {
                    // 如果用户输入了非法格式（比如在整型列输入了字母），在这里拦截并弹窗
                    MessageBox.Show(this, "输入的数据格式不正确，该列需要【" + fieldInfo.FieldType + "】类型数据！", "修改失败");
                }
            }
        }
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // 防御校验：跳过行头、无效行
            if (e.RowIndex < 0 || _currentDocument == null) return;

            // 1. 第 0 列 (中文翻译) 设置为淡灰色
            if (e.ColumnIndex == 0)
            {
                e.CellStyle.BackColor = Color.FromArgb(240, 240, 240);
                return;
            }
            else if (e.ColumnIndex == 1)
            {
                // 整型类型：浅蓝色
                e.CellStyle.BackColor = Color.FromArgb(230, 240, 255);
                return;
            }

            // 3. 中间的数据列：根据 DNT 字段类型动态变色
            var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex-1);
            if (fieldInfo != null)
            {
                switch (fieldInfo.FieldType)
                {
                    case DntFieldType.Text:
                        // 文本类型：浅黄色
                        e.CellStyle.BackColor = Color.FromArgb(255, 253, 232);
                        break;

                    case DntFieldType.BooleanInt:
                        // 布尔类型：浅绿色
                        e.CellStyle.BackColor = Color.FromArgb(230, 245, 230);
                        break;

                    case DntFieldType.Int32:
                        // 整型类型：浅蓝色
                        e.CellStyle.BackColor = Color.FromArgb(230, 240, 255);
                        break;

                    case DntFieldType.Percentage:
                        // 百分比类型：浅橙色
                        e.CellStyle.BackColor = Color.FromArgb(255, 240, 230);
                        break;

                    case DntFieldType.Float:
                        // 浮点数类型：浅紫色
                        e.CellStyle.BackColor = Color.FromArgb(245, 235, 255);
                        break;
                }
            }
        }
        /// <summary>
        /// 点击行头全选整行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 检查点击的是否是有效的列（排除了左上角的空白块）
            if (e.ColumnIndex >= 0)
            {
                // 1. 清除之前的所有选中状态
                dataGridView1.ClearSelection();

                //dataGridView1.Columns[e.ColumnIndex].Selected = true;

                // 遍历所有行，将该列的每一个单元格都设为选中
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    row.Cells[e.ColumnIndex].Selected = true;
                }

                // 3. 建议：将当前焦点单元格也移到该行的第一列（防止焦点框残留在其他地方）

            }
        }
        private void UpdateRowHeaderWidth()
        {
            // 如果没有数据，恢复默认宽度
            if (dataGridView1.Rows.Count == 0)
            {
                dataGridView1.RowHeadersWidth = 41; // 默认宽度
                return;
            }

            // 1. 获取最大行号的字符串（例如有100行，最大就是"100"）
            string maxRowNumber = dataGridView1.Rows.Count.ToString();

            // 2. 使用 DataGridView 的 Graphics 对象测量这个字符串的实际像素宽度
            using (Graphics g = dataGridView1.CreateGraphics())
            {
                Font rowFont = dataGridView1.RowHeadersDefaultCellStyle.Font ?? dataGridView1.Font;
                SizeF size = g.MeasureString(maxRowNumber, rowFont);

                // 3. 核心：最终宽度 = 数字宽度 + 预留给小三角的空间(20像素) + 左右边距(12像素)
                int requiredWidth = (int)size.Width + 20 + 12;

                // 4. 设置固定的最小宽度，防止数字太少时行头太窄不好看
                if (requiredWidth < 41) requiredWidth = 41;

                // 5. 关闭自带的自动变宽模式，防止冲突，然后赋值
                dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
                dataGridView1.RowHeadersWidth = requiredWidth;
            }
        }
        #endregion
        #region 拖拽功能
        // 当用户拖拽文件进入窗口边界时
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // 如果拖进来的是文件，改变鼠标图标允许放置
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        // 当用户在窗口内松开鼠标完成放置时
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string filePath = files[0];

                // 严谨校验：必须是 .dnt 后缀
                if (Path.GetExtension(filePath).ToLower() == ".dnt")
                {
                    ExecuteLoadDntFile(filePath);
                }
                else
                {
                    MessageBox.Show(this, "仅支持读取 .dnt 格式的二进制文件！", "格式错误");
                }
            }
        }
        #endregion
        #region dnt目录
        /// <summary>
        /// 加载DNT文件列表并匹配INI翻译
        /// </summary>
        /// <param name="folderPath">DNT文件所在的文件夹路径</param>
        private void LoadDntFilesToGrid(string folderPath)
        {
            // 1. 基础安全校验
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }
            // 2. 定位并读取翻译 INI 文件 (假设翻译文件放在软件同级目录下)
            Dictionary<string, string> translationDict = GlobalHelper.GetTranslationDict(GlobalHelper.AppRootPath, "dnt翻译.ini");

            if (translationDict==null)
            {
                return;
            }
            // 3. 获取目标路径下所有的 .dnt 文件
            string[] dntFiles = Directory.GetFiles(folderPath, "*.dnt", SearchOption.TopDirectoryOnly);

            // 4. 开始匹配并组装数据集
            List<DntFileItem> itemList = new List<DntFileItem>();
            int serialNumber = 1;

            foreach (string filePath in dntFiles)
            {
                // 获取带后缀的文件名（例如: skill.dnt）
                string fileNameWithExt = Path.GetFileName(filePath);

                // 从字典中匹配翻译，如果匹配不到则显示“未翻译”
                string chineseName = translationDict.ContainsKey(fileNameWithExt)
                    ? translationDict[fileNameWithExt]
                    : "";

                itemList.Add(new DntFileItem
                {
                    Index = serialNumber++,
                    FileName = fileNameWithExt,
                    ChineseName = chineseName
                });
            }

            // 5. 渲染绑定到 dataGridView2
            BindToDataGridView(itemList);
        }
        private void BindToDataGridView(List<DntFileItem> items)
        {
            // 清空旧数据和旧列头
            dataGridView2.DataSource = null;
            dataGridView2.Columns.Clear();

            // 开启双缓冲，防止大数量滚动时表格闪烁（硬核优化）
            Type dgvType = dataGridView2.GetType();
            System.Reflection.PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi?.SetValue(dataGridView2, true, null);

            // 绑定新数据源
            dataGridView2.DataSource = items;

            // 汉化并配置列名
            dataGridView2.Columns["Index"].HeaderText = "序列";
            dataGridView2.Columns["Index"].Width = 40; // 序列列可以窄一点

            dataGridView2.Columns["FileName"].HeaderText = "文件名称";
            dataGridView2.Columns["FileName"].Width = 200;

            dataGridView2.Columns["ChineseName"].HeaderText = "中文名称";

            // 让最后一列“中文名称”自动填满剩余的所有表格宽度，美化排版
            dataGridView2.Columns["ChineseName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // 禁止用户在表格最后一行手动添加空白新行
            dataGridView2.AllowUserToAddRows = false;
            // 设置为只能整行选择，不能单个单元格选择，更符合目录操作习惯
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.ReadOnly = true;
        }
        #endregion
        private void 打开DNT目录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换 Pane2 的显示和隐藏状态
            panel3.Visible = !panel3.Visible;

        }

        private void 打开关闭工具栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换 Pane2 的显示和隐藏状态
            panel2.Visible = !panel2.Visible;
        }

        private void 查找ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (searchForm == null || searchForm.IsDisposed)
            {
                searchForm = new SearchForm();

                // 核心设置：不在 Windows 任务栏中显示此窗口
                searchForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                searchForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                searchForm.Show(this);
            }
            else
            {
                searchForm.Activate();
            }
        }

        private void 设置目录配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (setingForm == null || setingForm.IsDisposed)
            {
                setingForm = new SetingForm(this);

                // 核心设置：不在 Windows 任务栏中显示此窗口
                setingForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                setingForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                setingForm.Show(this);
            }
            else
            {
                setingForm.Activate();
            }
        }

        private void 批量加密解密ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (batchCryptoForm == null || batchCryptoForm.IsDisposed)
            {
                batchCryptoForm = new BatchCryptoForm();

                // 核心设置：不在 Windows 任务栏中显示此窗口
                batchCryptoForm.ShowInTaskbar = false;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                batchCryptoForm.Show(this);
            }
            else
            {
                batchCryptoForm.Activate();
            }
        }

        private void 一键制作翻译源文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IniTranslationForm1 == null || IniTranslationForm1.IsDisposed)
            {
                IniTranslationForm1 = new IniTranslationForm1();

                IniTranslationForm1.ShowInTaskbar = false;

                IniTranslationForm1.Show(this);
            }
            else
            {
                IniTranslationForm1.Activate();
            }
        }

        private void 制作其他翻译源文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IniTranslationForm2 == null || IniTranslationForm2.IsDisposed)
            {
                IniTranslationForm2 = new IniTranslationForm2();

                IniTranslationForm2.ShowInTaskbar = false;

                IniTranslationForm2.Show(this);
            }
            else
            {
                IniTranslationForm2.Activate();
            }
        }

        private void dNT目录批量检索ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dntDeepSearchForm == null || dntDeepSearchForm.IsDisposed)
            {
                dntDeepSearchForm = new DntDeepSearchForm();

                dntDeepSearchForm.ShowInTaskbar = false;

                dntDeepSearchForm.Show(this);
            }
            else
            {
                dntDeepSearchForm.Activate();
            }
        }

        private void pAK补丁制作ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PakPackerForm == null || PakPackerForm.IsDisposed)
            {
                PakPackerForm = new PakPackerForm();

                PakPackerForm.ShowInTaskbar = false;

                PakPackerForm.Show(this);
            }
            else
            {
                PakPackerForm.Activate();
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 弹出提示框
            DialogResult result = MessageBox.Show(
                "确定要退出系统吗？未保存的数据可能会丢失。",
                "退出提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // 如果用户点击了“否”，则取消关闭操作
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void 快速保存ctrlsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentDocument==null)
            {
                return;
            }

            if (string.IsNullOrEmpty(AppConfig.PlainPath) || (AppConfig.IsSyncSaveEnabled && string.IsNullOrEmpty(AppConfig.CipherPath)))
            {
                DialogResult result1 = MessageBox.Show(
                "目录没有设置，请先到[设置目录]菜单中完成设置。是否现在前往设置",
                "系统提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
                );

                // 当用户点击了确定按钮后
                if (result1 == DialogResult.Yes)
                {
                    设置目录配置ToolStripMenuItem_Click(sender, e);
                }
                return;
            }
            // 1. 弹出询问对话框，设置标题、按钮（YesNoCancel）和警告图标
            DialogResult result = MessageBox.Show(
                "是否保存当前修改？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {
                // 用户点击了 “是”：执行你的保存方法
                DntConvertHelpers.SaveToFile(_currentDocument, AppConfig.PlainPath + "\\" + _currentLoadedFileName);
                if (AppConfig.IsSyncSaveEnabled)
                {
                    DntConvertHelpers.SaveToFile(_currentDocument, AppConfig.CipherPath + "\\" + _currentLoadedFileName,true);
                    MessageBox.Show("同时保存[明文/密文]成功！文件保存在:[" + AppConfig.PlainPath+"] 、["+AppConfig.CipherPath+"]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("保存成功！文件保存在:" + AppConfig.PlainPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
        }
        private void openDntfile(string path="")
        {
            string filePath = GlobalHelper.SelectFile("选择DNT文件", "DNT数据文件 (*.dnt)", "*.dnt", path);
            if (!string.IsNullOrEmpty(filePath))
            {
                // 严谨校验：必须是 .dnt 后缀
                if (Path.GetExtension(filePath).ToLower() == ".dnt")
                {
                    ExecuteLoadDntFile(filePath);
                }
                else
                {
                    MessageBox.Show(this, "仅支持读取 .dnt 格式的二进制文件！", "格式错误");
                }
            }
        }
        private void 选择文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openDntfile();
        }
        private void panel4_DoubleClick(object sender, EventArgs e)
        {
            openDntfile(AppConfig.DntPath);
        }
        #region 定位功能
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // 判断用户按下的按键是否为回车键 (Enter)
            if (e.KeyCode == Keys.Enter)
            {
                // 执行您想要调用的业务方法
                currentCell();

                // 【核心】阻止回车键触发系统默认的“叮”警告声，并防止多行文本框换行
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        private void currentCell()
        {
            string sIndex = textBox1.Text.Trim();
            if (string.IsNullOrWhiteSpace(sIndex)) return;

            int rowIndex = int.Parse(sIndex);
            if (dataGridView1.Rows.Count > rowIndex)
            {
                dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex-1].Cells[1];
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            currentCell();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // 如果只需要整数，移除所有非数字字符
            string digitsOnly = System.Text.RegularExpressions.Regex.Replace(textBox.Text, @"[^\d]", "");

            // 如果允许小数，可以使用下面的正则（可选）：
            // string digitsOnly = System.Text.RegularExpressions.Regex.Replace(textBox.Text, @"[^\d.]", "");

            if (textBox.Text != digitsOnly)
            {
                textBox.Text = digitsOnly;
                // 将光标移至文本末尾，防止光标跳到最前面
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // char.IsDigit(e.KeyChar) 检查是否为数字
            // e.KeyChar == (char)Keys.Back 检查是否为退格键（允许删除错字）
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
            {
                // 设置为 true 表示事件已处理，从而丢弃当前按下的按键
                e.Handled = true;
            }
        }
        #endregion
        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            // 1. 定义要显示的行号文本（e.RowIndex 从 0 开始，所以计数要 + 1）
            string rowNumber = (e.RowIndex + 1).ToString();

            // 2. 设置行号的字体和颜色（这里使用 DataGridView 默认的字体和文本颜色）
            Font rowFont = this.dataGridView1.RowHeadersDefaultCellStyle.Font ?? this.dataGridView1.Font;
            Brush rowBrush = new SolidBrush(this.dataGridView1.RowHeadersDefaultCellStyle.ForeColor);

            // 3. 计算文本绘制的精准坐标，使其居中显示
            // 计算文字的宽度和高度
            var textSize = e.Graphics.MeasureString(rowNumber, rowFont);

            // X 坐标：行头矩形左边界 + (行头宽度 - 文字宽度) / 2
            float x = e.RowBounds.Left + (this.dataGridView1.RowHeadersWidth - textSize.Width) / 2;
            // Y 坐标：行头矩形上边界 + (行高 - 文字高度) / 2
            float y = e.RowBounds.Top + (e.RowBounds.Height - textSize.Height) / 2;

            // 4. 在行头区域绘制文本
            e.Graphics.DrawString(rowNumber, rowFont, rowBrush, x, y);
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentDocument == null)
            {
                return;
            }
            // 1. 弹出询问对话框，设置标题、按钮（YesNoCancel）和警告图标
            DialogResult result = MessageBox.Show(
                "是否覆盖文件？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {
                // 用户点击了 “是”：执行你的保存方法
                DntConvertHelpers.SaveToFile(_currentDocument, _currentLoadedFilePath);
                MessageBox.Show("保存成功！文件已覆盖:" + _currentLoadedFilePath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
        }

        private void 保存至密文目录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentDocument == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(AppConfig.CipherPath))
            {
                DialogResult result1 = MessageBox.Show(
                "密文目录没有设置，请先到[设置目录]菜单中完成设置。是否现在前往设置",
                "系统提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
                );

                // 当用户点击了确定按钮后
                if (result1 == DialogResult.Yes)
                {
                    设置目录配置ToolStripMenuItem_Click(sender,e);
                }
                return;
            }
            // 1. 弹出询问对话框，设置标题、按钮（YesNoCancel）和警告图标
            DialogResult result = MessageBox.Show(
                "是否保存当前修改？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {
                // 用户点击了 “是”：执行你的保存方法
                DntConvertHelpers.SaveToFile(_currentDocument, AppConfig.CipherPath + "\\" + _currentLoadedFileName,true);
                MessageBox.Show("保存成功！文件保存在:" + AppConfig.CipherPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
        }
    }
}
