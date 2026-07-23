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
using Microsoft.VisualBasic;

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
        private FilterForm filterForm = null;
        private QuickParamForm quickParamForm = null;
        private CalculatorForm calculatorForm = null;
        private DntTranConverForm dntTranConverForm = null;
        private AddColumnForm addColumnForm = null;

        public string _currentLoadedFilePath = string.Empty; // 记录当前打开的文件路径
        public string _currentLoadedFileName = string.Empty;// 记录当前打开的文件名
        public DntDocument _currentDocument = null;          // 记录当前打开的文档对象
        private translationDicts dicts = null;  //翻译字典
        private Dictionary<string,string> headDicts = null;  //翻译表头字典
        public Dictionary<string, ColumTranslationItem> colTranDict = null;//存放列翻译内容
        public List<uint> dList = null;

        // 声明右键菜单
        private ContextMenuStrip gridContextMenu;

        private Size lastSize;
        private bool isUpdateCellSelect = false;
        public MainForm(string filePath, int index)
        {
            InitializeComponent();
            AppConfig.Load();
            LoadDntFilesToGrid(AppConfig.DntPath);
            initilizationDicts();
            // 初始化右键菜单
            InitializeGridContextMenu();

            // 初始化时先记录一次初始尺寸
            lastSize = panel3.Size;

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
            this.dataGridView1.EditingControlShowing += dataGridView1_EditingControlShowing;
            this.dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
            this.dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
            // 绑定表格的鼠标按下事件
            this.dataGridView1.CellMouseDown += DataGridView1_CellMouseDown;
            // 2. 顺手绑定之前学过的快捷键监听事件（用来拦截 Ctrl+V 执行粘贴）
            this.dataGridView1.KeyDown += DataGridView1_KeyDown;

            this.textBox1.KeyPress += textBox1_KeyPress;
            this.textBox1.TextChanged += textBox1_TextChanged;

            ExecuteLoadDntFile(filePath);
            textBox1.Text = (index+1).ToString();
            currentCell();
        }
        private void DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            // 监听 Ctrl + V 粘贴快捷键
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteClipboardData();
                e.Handled = true; // 阻止系统默认按键音
            }
        }
        private void PasteClipboardData()
        {
            // 1. 检查剪贴板中是否有文本内容
            if (!Clipboard.ContainsText()) return;

            // 2. 获取当前选中的起始单元格
            if (dataGridView1.CurrentCell == null) return;
            int startRow = dataGridView1.CurrentCell.RowIndex;
            int startCol = dataGridView1.CurrentCell.ColumnIndex;

            // 3. 获取剪贴板文本，并按行切分
            string clipboardText = Clipboard.GetText();
            string[] lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                // 过滤最后可能存在的空行
                if (string.IsNullOrWhiteSpace(lines[i]) && i == lines.Length - 1) break;

                int targetRow = startRow + i;

                // 如果粘贴的数据超出了表格当前的最大行数
                if (targetRow >= dataGridView1.RowCount)
                {
                    // 如果允许用户添加新行，则自动追加一行，否则跳过
                    if (dataGridView1.AllowUserToAddRows)
                    {
                        dataGridView1.Rows.Add();
                    }
                    else
                    {
                        break;
                    }
                }

                // 按制表符（\t）切分当前行的每一列
                string[] cells = lines[i].Split('\t');

                for (int j = 0; j < cells.Length; j++)
                {
                    int targetCol = startCol + j;

                    // 确保不超出表格的总列数
                    if (targetCol >= dataGridView1.ColumnCount) break;

                    // 检查目标单元格是否允许编辑（只读单元格不覆盖）
                    if (!dataGridView1.Rows[targetRow].Cells[targetCol].ReadOnly)
                    {
                        // 将剪贴板的值填入单元格
                        dataGridView1.Rows[targetRow].Cells[targetCol].Value = cells[j];
                    }
                }
            }
        }
        // 关键点 2：在单元格上按下鼠标时触发
        private void DataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 确保用户点击的是鼠标右键
            if (e.Button == MouseButtons.Right)
            {
                // e.RowIndex >= 0 确保点击的是有效数据行（排除列头 RowIndex = -1）
                // e.ColumnIndex >= 0 确保排除行头（ColumnIndex = -1）
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // 1. 清除当前表格中所有已选中的行
                    dataGridView1.ClearSelection();

                    // 2. 强行将鼠标右键点击的那一行设为选中状态（实现右键自动高亮）
                    dataGridView1.Rows[e.RowIndex].Selected = true;

                    // 3. 强行将当前活动单元格设为点击的单元格（可选，方便后续直接用 CurrentRow 获取数据）
                    dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                    // 4. 在鼠标当前点击的坐标位置弹出右键菜单
                    gridContextMenu.Show(MousePosition);
                }
            }
        }
        private void InitializeGridContextMenu()
        {
            gridContextMenu = new ContextMenuStrip();

            // 创建菜单项
            ToolStripMenuItem insUpkItem = new ToolStripMenuItem("向上插入一行");
            ToolStripMenuItem insDownItem = new ToolStripMenuItem("向下插入一行(ins)");
            ToolStripMenuItem insDownNItem = new ToolStripMenuItem("向下插入N行");
            ToolStripMenuItem delItem = new ToolStripMenuItem("删除选中行(del)");
            ToolStripMenuItem copyItem = new ToolStripMenuItem("复制 (Ctrl+C)");
            ToolStripMenuItem pasteItem = new ToolStripMenuItem("粘贴 (Ctrl+V)");
            ToolStripMenuItem calcItem = new ToolStripMenuItem("批量计算");
            ToolStripMenuItem searchItem = new ToolStripMenuItem("查找");
            ToolStripMenuItem filterItem = new ToolStripMenuItem("筛选");
            ToolStripMenuItem tranColItem = new ToolStripMenuItem("翻译列 (F1)");
            ToolStripMenuItem tranCellItem = new ToolStripMenuItem("翻译单元格 (F2)");
            ToolStripMenuItem tranCol2Item = new ToolStripMenuItem("翻译列-覆盖 (F3)");
            ToolStripMenuItem tranHeadItem = new ToolStripMenuItem("翻译标题 (F4)");
            ToolStripMenuItem addColItem = new ToolStripMenuItem("增加列");
            ToolStripMenuItem delColItem = new ToolStripMenuItem("删除列");

            // 绑定事件
            //packItem.Click += PackItem_Click;
            //openItem.Click += OpenItem_Click;
            copyItem.Click += (s, e) => {
                // 调用 DataGridView 的原生复制方法
                if (dataGridView1.GetCellCount(DataGridViewElementStates.Selected) > 0)
                {
                    Clipboard.SetDataObject(dataGridView1.GetClipboardContent());
                }
            };
            pasteItem.Click += (s, e) => PasteClipboardData();
            insUpkItem.Click += 向上插入一行ToolStripMenuItem_Click;
            insDownItem.Click += 向下插入一行insToolStripMenuItem_Click;
            insDownNItem.Click += 向下插入N行ToolStripMenuItem_Click;
            delItem.Click += 删除当前行ToolStripMenuItem_Click;
            calcItem.Click += 计算ToolStripMenuItem_Click;
            searchItem.Click += 查找ToolStripMenuItem_Click;
            filterItem.Click += 筛选ToolStripMenuItem_Click;
            tranColItem.Click += button1_Click;
            tranCellItem.Click += (s, e) =>
             {
                 checkBox3.Checked = !checkBox3.Checked;
             };
            tranCol2Item.Click += button4_Click;
            tranHeadItem.Click += (s, e) =>
            {
                checkBox2.Checked = !checkBox2.Checked;
            };
            addColItem.Click += 增加列ToolStripMenuItem_Click;
            delColItem.Click += 删除列ToolStripMenuItem_Click;
            // 添加到菜单

            // 【核心代码】：在这里插入一条横向分割线，把基础操作和业务操作分开

            gridContextMenu.Items.Add(copyItem);
            gridContextMenu.Items.Add(pasteItem);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add(insDownItem);
            gridContextMenu.Items.Add(insDownNItem);
            gridContextMenu.Items.Add(insUpkItem);
            gridContextMenu.Items.Add(delItem);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add(searchItem);
            gridContextMenu.Items.Add(filterItem);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add(addColItem);
            gridContextMenu.Items.Add(delColItem);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add(tranColItem);
            gridContextMenu.Items.Add(tranCellItem);
            gridContextMenu.Items.Add(tranCol2Item);
            gridContextMenu.Items.Add(tranHeadItem);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add(calcItem);
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

            // 防御：排除表头
            if (e.RowIndex < 0 || e.ColumnIndex < 1) return;
            if (!checkBox3.Checked) return;

            string nameIdKey = null;
            if (e.ColumnIndex==1)
            {
                nameIdKey = _currentDocument.Columns["PKID"][e.RowIndex]?.ToString()?.Trim() ?? string.Empty;
            }
            else
            {
                var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex);
                nameIdKey = _currentDocument.Columns[fieldInfo.FieldName][e.RowIndex]?.ToString()?.Trim() ?? string.Empty;
            }
            
            dicts.translationDict.TryGetValue(nameIdKey, out string templateText);

            // 1. 获取屏幕物理坐标
            Point mouseScreenPos = Control.MousePosition;

            // 2. 直接转为主窗口相对坐标（系统会自动处理 150% 缩放）
            Point formRelativePos = this.PointToClient(mouseScreenPos);

            // 3. ⚠️ 拒绝任何手动除以 1.5 或乘以 1.5 的计算！直接加上偏移量即可
            int finalX = formRelativePos.X;
            int finalY = formRelativePos.Y + 80; // 鼠标下方 20 像素

            // 4. 宿主必须是 this
            toolTip1.Show((templateText??"无"), this, finalX, finalY, 3000);
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
            if (string.IsNullOrEmpty(filePath)) return;
            // 1. 初始化并启动秒表
            titleStatusLabel.stopwatch = new Stopwatch();
            titleStatusLabel.stopwatch.Start();
            try
            {
                _currentLoadedFilePath = filePath;
                _currentLoadedFileName = Path.GetFileName(filePath);

                // 核心读取
                _currentDocument = DntConvertHelpers.LoadFromFile(filePath);

                colTranDict = new Dictionary<string, ColumTranslationItem>();
                //初始化colTranDict列翻译存储列表
                foreach (string colKey in _currentDocument.Columns.Keys)
                {
                    colTranDict.Add(colKey,new ColumTranslationItem());
                }
                dList = new List<uint>();
                for (uint i = 0; i < _currentDocument.RecordCount; i++)
                {
                    dList.Add(i);
                }
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

                titleStatusLabel.fileName = Path.GetFileName(filePath);
                this.Text = titleStatusLabel.toTitle();
                this.checkBox2.Checked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"读取DNT文件发生错误: {ex.Message}", "读取失败");
            }
            finally
            {
                // 3. 无论成功或失败，停止秒表
                titleStatusLabel.stopwatch.Stop();
            }
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();
        }
        #endregion
        #region datagridview1虚拟化
        /// <summary>
        /// 初始化并开启虚拟模式表格
        /// </summary>
        public void InitVirtualDataGridView(DntDocument doc)
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
                ReadOnly = true, // 翻译列由 ini 配置决定，设为只读
                Frozen = true
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
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    //AutoSizeMode = (DataGridViewAutoSizeColumnMode)DataGridViewAutoSizeColumnsMode.ColumnHeader
                };
                if (field.FieldType == DntFieldType.Percentage) 
                {
                    dataColumn.DefaultCellStyle.Format = "0.#######";
                }
                dataGridView1.Columns.Add(dataColumn);
            }

            // 4. 核心：告诉壳子表格一共有多少行数据，表格会自动生成垂直滚动条
            dataGridView1.RowCount = (int)doc.RecordCount;
        }
        // 当表格需要渲染某个格子时，会自动触发这个事件
        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            // 核心更改 1：行数的防御性校验。总行数现在直接读取 "PKID" 这一列的长度
            if (dataGridView1.RowCount == 0) return;
            if (_currentDocument == null || e.RowIndex < 0) return;

            var pkidList = _currentDocument.Columns["PKID"] as List<uint>;
            if (pkidList == null || e.RowIndex >= pkidList.Count) return;

            int realIndex = (int)dList[e.RowIndex];

            // 核心更改 3：传入 e.ColumnIndex - 1，从而在 GetFieldAt 内部正确跳过最前面的虚拟翻译列
            var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex);
            // 情况 A：第 0 列 —— 最左侧中文翻译列
            if (e.ColumnIndex == 0)
            {
                var transList = _currentDocument.Columns[fieldInfo.FieldName] as List<string>;
                if (transList.Count==0)
                {
                    e.Value = "";
                }
                else
                {
                    e.Value = transList[realIndex];
                    
                }
            }
            // 情况 B：第 1 列 —— PKID 主键列
            else if (e.ColumnIndex == 1)
            {
                var ColTransItem = colTranDict[fieldInfo.FieldName] as ColumTranslationItem;
                if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[e.RowIndex]))
                {
                    e.Value = "T:" + ColTransItem.TranslatedTextList[realIndex];
                }
                else
                {
                    // 核心更改 2：直接去分布式字典里的 "PKID" 强类型列中取数
                    e.Value = pkidList[realIndex];
                }
            }
            // 情况 C：第 2 列及往后 —— DNT 常规数据列
            else
            {

                if (fieldInfo != null && _currentDocument.Columns.ContainsKey(fieldInfo.FieldName))
                {
                    var ColTransItem = colTranDict[fieldInfo.FieldName] as ColumTranslationItem;
                    if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[e.RowIndex]))
                    {
                        e.Value = "T:" + ColTransItem.TranslatedTextList[realIndex];
                    }
                    else
                    {
                        // 列式取数：找到该列对应的 IList 列表，拿出它的第 RowIndex 行数据
                        e.Value = _currentDocument.Columns[fieldInfo.FieldName][realIndex];
                    }
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

            if (e.ColumnIndex > 0)
            {
                var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex);
                try
                {
                    string userInput = e.Value?.ToString() ?? string.Empty;
                    if (e.ColumnIndex == 1)
                    {
                        pkidList[e.RowIndex] = uint.Parse(userInput);
                    }
                    // 传入 e.ColumnIndex - 1，在 GetFieldAt 内部正确跳过最前方的虚拟翻译列，从而拿到 DNT 列定义
                    
                    if (fieldInfo == null) return;

                    // 核心更改 3：通过字段名称直接从分布式 Columns 字典中提取对应的列列表
                    var columnList = _currentDocument.Columns[fieldInfo.FieldName];


                    // 核心更改 4：在 .NET 4.7.2 下，必须先将 columnList 转换为对应的强类型 List<T> 才能通过索引赋值
                    switch (fieldInfo.FieldType)
                    {
                        case DntFieldType.Text:
                            var listText = columnList as List<string>;
                            if (listText != null) listText[e.RowIndex] = userInput;
                            break;

                        case DntFieldType.BooleanInt:
                            var listBool = columnList as List<int>;
                            if (listBool != null)
                            {
                                // 兼容输入 true/false 或 1/0
                                if (userInput == "1" || userInput == "0")
                                    listBool[e.RowIndex] = int.Parse(userInput);
                                else
                                    listBool[e.RowIndex] = int.Parse(userInput) > 0 ? 1 : 0;
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
                    MessageBox.Show(this, $"输入的数据格式不正确，该列({fieldInfo.FieldName})需要【" + fieldInfo.FieldType + "】类型数据！", "修改失败");
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
            var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex);
            if (fieldInfo != null)
            {
                var ColTransItem = colTranDict[fieldInfo.FieldName] as ColumTranslationItem;
                bool isTranslated = ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[e.RowIndex]);
                Color baseColor = Color.White;
                switch (fieldInfo.FieldType)
                {
                    case DntFieldType.Text: // 文本类型：浅黄色
                        // 原始浅黄 vs 翻译后极淡的麦芽黄
                        baseColor = Color.FromArgb(255, 253, 232);
                        //e.CellStyle.BackColor = isTranslated ? Color.FromArgb((255 + 190) / 2, (253 + 245) / 2, (232 + 235) / 2) : Color.FromArgb(255, 253, 232);
                        break;
                    case DntFieldType.BooleanInt:// 布尔类型：浅绿色
                        // 原始浅绿 vs 翻译后淡青灰
                        baseColor = Color.FromArgb(230, 245, 230);
                        //e.CellStyle.BackColor = isTranslated ? Color.FromArgb((255 + 190) / 2, (253 + 245) / 2, (232 + 235) / 2) : Color.FromArgb(230, 245, 230);
                       break;
                    case DntFieldType.Int32:// 整型类型：浅蓝色
                        // 原始浅蓝 vs 翻译后淡蓝灰
                        baseColor = Color.FromArgb(230, 240, 255);
                        //e.CellStyle.BackColor = isTranslated ? Color.FromArgb((255 + 190) / 2, (253 + 245) / 2, (232 + 235) / 2) : Color.FromArgb(230, 240, 255);
                        break;
                    case DntFieldType.Percentage: // 百分比类型：浅橙色
                        // 原始浅橙 vs 翻译后粉灰
                        baseColor = Color.FromArgb(255, 240, 230);
                        //e.CellStyle.BackColor = isTranslated ? Color.FromArgb((255 + 190) / 2, (253 + 245) / 2, (232 + 235) / 2) : Color.FromArgb(255, 240, 230);
                        break;
                    case DntFieldType.Float:// 浮点数类型：浅紫色
                        // 原始浅紫 vs 翻译后淡紫灰
                        baseColor = Color.FromArgb(245, 235, 255);
                        //e.CellStyle.BackColor = isTranslated ? Color.FromArgb((255 + 190) / 2, (253 + 245) / 2, (232 + 235) / 2) : Color.FromArgb(245, 235, 255);
                        break;
                }
                if (isTranslated)
                {
                    // 🔥 【魔法混色】将原色与一种明亮的青绿色(190, 245, 235)进行线性混合（各占50%）
                    // 这样既保留了数据类型的色调，又打上了“已翻译”的冷色高亮标签
                    e.CellStyle.BackColor = Color.FromArgb(
                        (baseColor.R + 190) / 2,
                        (baseColor.G + 245) / 2,
                        (baseColor.B + 235) / 2
                    );
                }
                else
                {
                    // 未翻译，保持原色
                    e.CellStyle.BackColor = baseColor;
                }

            }
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex==0)
            {

            }
            else if (e.ColumnIndex==1)
            {
                var ColTransItem = colTranDict["PKID"] as ColumTranslationItem;
                if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[e.RowIndex]))
                {
                    ColTransItem.TranslatedTextList[e.RowIndex] = "";
                }
            }
            else
            {
                var fieldInfo = _currentDocument.GetFieldAt(e.ColumnIndex);
                if (fieldInfo != null && _currentDocument.Columns.ContainsKey(fieldInfo.FieldName))
                {
                    var ColTransItem = colTranDict[fieldInfo.FieldName] as ColumTranslationItem;
                    if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[e.RowIndex]))
                    {
                        ColTransItem.TranslatedTextList[e.RowIndex] = "";
                    }
                }
            }
        }
        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // 编辑结束后，刷新当前行，这会强制触发 CellValueNeeded，让界面变回 [翻译后文本]
            dataGridView1.InvalidateRow(e.RowIndex);
        }
        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            int colIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;

            // 确保当前正在编辑的控件是文本框
            if (e.Control is TextBox textBox)
            {
                string rawValue = null;

                if (colIndex == 0)
                {
                    // 第 0 列的原始数据逻辑（如果有）
                }
                else if (colIndex == 1)
                {
                    var transList = _currentDocument.Columns["PKID"] as List<string>;
                    var ColTransItem = colTranDict["PKID"] as ColumTranslationItem;
                    if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[rowIndex]))
                    {
                        rawValue = transList[rowIndex];
                    }
                }
                else
                {
                    var fieldInfo = _currentDocument.GetFieldAt(colIndex);
                    if (fieldInfo != null && _currentDocument.Columns.ContainsKey(fieldInfo.FieldName))
                    {
                        var ColTransItem = colTranDict[fieldInfo.FieldName] as ColumTranslationItem;
                        if (ColTransItem.isTrans && !string.IsNullOrEmpty(ColTransItem.TranslatedTextList[rowIndex]))
                        {
                            rawValue = _currentDocument.Columns[fieldInfo.FieldName][rowIndex]?.ToString();
                        }
                    }
                }

                // 🔥 【核心】如果找到了原始数据，直接强行改变编辑框正在显示的文本！
                if (rawValue != null)
                {
                    textBox.Text = rawValue;
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
            isUpdateCellSelect = true;
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
            isUpdateCellSelect = false;
            dataGridView1_SelectionChanged(sender, (EventArgs)e);
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
            Dictionary<string, string> translationDict = GlobalHelper.GetTranslationDict(GlobalHelper.AppRootPath, translationDicts.dntTransFileName);

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
            if (panel3.Visible)
            {
                this.Width += panel3.Width;
            }
            else
            {
                this.Width -= panel3.Width;
            }
            

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
                searchForm = new SearchForm(this);

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
                IniTranslationForm1 = new IniTranslationForm1(dicts);

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
                IniTranslationForm2 = new IniTranslationForm2(this);

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
            if (setingForm == null || setingForm.IsDisposed)
            {
                setingForm = new SetingForm(this);

                // 核心设置：不在 Windows 任务栏中显示此窗口
                setingForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                setingForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                setingForm.Show(this);

                setingForm.button20_Click(sender,e);
            }
            else
            {
                setingForm.Activate();
                setingForm.button20_Click(sender, e);
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
            //openDntfile(AppConfig.DntPath);
            button2_Click(sender, e);
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

        private void initilizationDicts()
        {
            /***
            dicts = new translationDicts();
            string filePath = GlobalHelper.AppRootPath + translationDicts.SourcePath + translationDicts.UistringSource + ".ini";
            dicts.translationDict = GlobalHelper.LoadIniTranslation(filePath);

            titleStatusLabel.sourceFile = translationDicts.UistringSource;
            this.Text = titleStatusLabel.toTitle();
            ***/
            loadCombobox1();
            comboBox1.SelectedValue = translationDicts.UistringSource;
        }
        private void loadCombobox1()
        {
            // 1.创建一个列表用于存放列对象
            List<sourceFilePathItem> columnItems = new List<sourceFilePathItem>();
            string sourcePath = GlobalHelper.AppRootPath + translationDicts.SourcePath;
            //uistring
            //itemtable
            //skilltable
            //maptable
            //monstertable
            //npctable
            columnItems.Add(new sourceFilePathItem
            {//uistring
                HeaderText = "uistring", // 比如 "列1"、"主键ID"
                filePath = sourcePath+ translationDicts.UistringSource+".ini",
                PKName = translationDicts.UistringSource
            });
            columnItems.Add(new sourceFilePathItem
            {//itemtable
                HeaderText = "itemtable", // 比如 "列1"、"主键ID"
                filePath = sourcePath + translationDicts.ItemSource + ".ini",
                PKName = translationDicts.ItemSource      // 比如 "Column1"、"PKID"
            });
            columnItems.Add(new sourceFilePathItem
            {//skilltable
                HeaderText = "skilltable", // 比如 "列1"、"主键ID"
                filePath = sourcePath + translationDicts.SkillSource + ".ini",
                PKName = translationDicts.SkillSource      // 比如 "Column1"、"PKID"
            });
            columnItems.Add(new sourceFilePathItem
            {//maptable
                HeaderText = "maptable", // 比如 "列1"、"主键ID"
                filePath = sourcePath + translationDicts.MapSource + ".ini",
                PKName = translationDicts.MapSource      // 比如 "Column1"、"PKID"
            });
            columnItems.Add(new sourceFilePathItem
            {//monstertable
                HeaderText = "monstertable", // 比如 "列1"、"主键ID"
                filePath = sourcePath + translationDicts.MonsterSource + ".ini",
                PKName = translationDicts.MonsterSource      // 比如 "Column1"、"PKID"
            });
            columnItems.Add(new sourceFilePathItem
            {//npctable
                HeaderText = "npctable", // 比如 "列1"、"主键ID"
                filePath = sourcePath + translationDicts.NPCSource + ".ini",
                PKName = translationDicts.NPCSource      // 比如 "Column1"、"PKID"
            });

            string otherSourPath = sourcePath + translationDicts.otherSourcePath;
            // 仅获取该文件夹下的 .ini 文件
            string[] iniFiles = Directory.GetFiles(otherSourPath, "*.ini");

            for (int i = 0; i < iniFiles.Length; i++)
            {
                string filePath = iniFiles[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                columnItems.Add(new sourceFilePathItem
                {
                    HeaderText = translationDicts.otherSource + fileName, // 比如 "列1"、"主键ID"
                    filePath = fileName,
                    PKName = translationDicts.otherSource + fileName
                }) ;
            }

            // 3. 先解绑数据源，防止冲突
            comboBox1.DataSource = null;
            comboBox1.Items.Clear();

            // 4. 绑定提取出的列对象组
            comboBox1.DataSource = columnItems;
            comboBox1.DisplayMember = "HeaderText"; // 让用户在下拉框里看到易懂的表头文字
            comboBox1.ValueMember = "PKName";   // 后台隐藏实际的列名，方便后续写代码

        }
        private void 使用uistring源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.UistringSource);
        }
        private void translationCol()
        {
            // 1. UI 层的安全前置拦截
            if (this.dataGridView1.CurrentCell == null) return;

            int colIndex = this.dataGridView1.CurrentCell.ColumnIndex;
            string sourceColumnName = this.dataGridView1.Columns[colIndex].Name;
            if (colIndex == 0)
            {
                MessageBox.Show("翻译失败，[中文翻译]列不可以作为翻译目标", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                // 2. 刷新前清空网格绑定（如果是普通模式）或暂停重绘，虚拟模式可以直接往下走
                // this.dataGridView1.DataSource = null; 

                // 3. 一行代码调用模块化翻译核心函数
                int processedCount = dicts.TranslateColumnData(
                    _currentDocument,
                    sourceColumnName,
                    dicts.translationDict
                );

                // 4. UI 刷新反馈
                if (processedCount >= 0)
                {
                    if (this.dataGridView1.VirtualMode)
                    {
                        // 虚拟模式高性能通知重绘
                        this.dataGridView1.Invalidate();
                    }
                    else
                    {
                        this.dataGridView1.Refresh();
                    }

                    //MessageBox.Show($"翻译碰撞完成！已成功处理 {processedCount} 行数据。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("翻译失败，请检查数据核心列或目标翻译列是否初始化！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析嵌套文本时崩溃防御: {ex.Message}", "崩溃防御", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            translationCol();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            sourceFilePathItem item = (sourceFilePathItem)comboBox1.SelectedItem;
            if (item==null)
            {
                return;
            }
            if (dicts==null)
            {
                dicts = new translationDicts();
            }
            string filePath = item.filePath;
            dicts.translationDict = GlobalHelper.LoadIniTranslation(filePath);

            titleStatusLabel.sourceFile = item.PKName;
            this.Text = titleStatusLabel.toTitle();

        }

        private void changeDicts(string source)
        {   /***
            if (dicts == null)
            {
                dicts = new translationDicts();
            }
            string filePath = GlobalHelper.AppRootPath + translationDicts.SourcePath + source + ".ini";
            dicts.translationDict = GlobalHelper.LoadIniTranslation(filePath);
            if (dicts.translationDict == null)
            {
                MessageBox.Show($"切换{source}翻译源失败,请先创建翻译源文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }***/
            titleStatusLabel.sourceFile = source;
            this.Text = titleStatusLabel.toTitle();
            comboBox1.SelectedValue = source;
        }
        private void 使用物品源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.ItemSource);
        }

        private void 使用技能源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.SkillSource);
        }

        private void 使用地图源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.MapSource);
        }

        private void 使用怪物源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.MonsterSource);
        }

        private void 使用npc源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeDicts(translationDicts.NPCSource);
        }

        private void 使用其他源ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string otherPath = GlobalHelper.AppRootPath + "database\\" + translationDicts.otherSourcePath;
            string filePath = GlobalHelper.SelectFile("选择其他翻译源文件", "翻译源文件 (*.ini)", "*.ini", otherPath);
            if (!string.IsNullOrEmpty(filePath))
            {
                // 严谨校验：必须是 .dnt 后缀
                if (Path.GetExtension(filePath).ToLower() == ".ini")
                {
                    loadCombobox1();
                    string source = translationDicts.otherSource + Path.GetFileNameWithoutExtension(filePath);

                    titleStatusLabel.sourceFile = source;
                    this.Text = titleStatusLabel.toTitle();
                    comboBox1.SelectedValue = source;
                }
                else
                {
                    MessageBox.Show(this, "仅支持读取 .ini 格式的二进制文件！", "格式错误");
                }
            }
        }
        /// <summary>
        /// 
        /// 翻译覆盖所选列(F3)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            // 1. UI 层的安全前置拦截
            if (this.dataGridView1.CurrentCell == null) return;

            int colIndex = this.dataGridView1.CurrentCell.ColumnIndex;
            string sourceColumnName = this.dataGridView1.Columns[colIndex].Name;
            if (colIndex == 0)
            {
                MessageBox.Show("翻译失败，[中文翻译]列不可以作为翻译目标", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (colTranDict[sourceColumnName].isTrans)
            {
                colTranDict[sourceColumnName].isTrans = false;
                if (this.dataGridView1.VirtualMode)
                {
                    // 虚拟模式高性能通知重绘
                    this.dataGridView1.Invalidate();
                }
                else
                {
                    this.dataGridView1.Refresh();
                }
                return;
            }
            try
            {
                // 2. 刷新前清空网格绑定（如果是普通模式）或暂停重绘，虚拟模式可以直接往下走
                // this.dataGridView1.DataSource = null; 

                // 3. 一行代码调用模块化翻译核心函数
                colTranDict[sourceColumnName].isTrans = true;
                int processedCount = dicts.OverTranslateColumnData(
                    _currentDocument,
                    sourceColumnName,
                    dicts.translationDict,
                    colTranDict[sourceColumnName]
                );

                // 4. UI 刷新反馈
                if (processedCount >= 0)
                {
                    if (this.dataGridView1.VirtualMode)
                    {
                        // 虚拟模式高性能通知重绘
                        this.dataGridView1.Invalidate();
                    }
                    else
                    {
                        this.dataGridView1.Refresh();
                    }

                    //MessageBox.Show($"翻译碰撞完成！已成功处理 {processedCount} 行数据。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    colTranDict[sourceColumnName].isTrans = false;
                    MessageBox.Show("翻译失败，请检查数据核心列或目标翻译列是否初始化！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析嵌套文本时崩溃防御: {ex.Message}", "崩溃防御", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                int colCount = _currentDocument.FieldCount;
                if (checkBox2.Checked && colCount >= 2)
                {//翻译表头
                    if (headDicts == null)
                    {
                        headDicts = new Dictionary<string, string>();
                    }
                    headDicts = GlobalHelper.LoadIniTranslation(GlobalHelper.AppRootPath + translationDicts.dntHeadTransFileName);
                    foreach (var item in _currentDocument.Fields)
                    {
                        string FieldName = item.FieldName;
                        headDicts.TryGetValue(FieldName, out string chineseName);
                        if (!string.IsNullOrEmpty(chineseName))
                        {
                            dataGridView1.Columns[FieldName].HeaderText = chineseName;
                        }
                    }
                    // 强制整个控件重新绘制（双重保险）
                    dataGridView1.Refresh();
                }
                else
                {//显示表头原文
                    int colIndex = 2;
                    foreach (var item in _currentDocument.Fields)
                    {
                        string FieldName = item.FieldName;
                        dataGridView1.Columns[FieldName].HeaderText = FieldName;
                    }
                    //刷新
                    // 强制整个控件重新绘制（双重保险）
                    dataGridView1.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析翻译文本时崩溃防御: {ex.Message}", "崩溃防御", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
           
        }

        private void 筛选ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filterForm == null || filterForm.IsDisposed)
            {
                filterForm = new FilterForm(this);

                // 核心设置：不在 Windows 任务栏中显示此窗口
                filterForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                filterForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                filterForm.Show(this);
            }
            else
            {
                filterForm.Activate();
            }
        }
        private void datagridview_removeRow()
        {
            if (_currentDocument == null) return;
            var colIndex1 = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex1 = dataGridView1.CurrentCell.RowIndex;

            List<int> selectedRowIndices = dataGridView1.SelectedCells
                    .Cast<DataGridViewCell>()
                    .Select(cell => cell.RowIndex)
                    .Distinct()
                    .OrderByDescending(rowIndex => rowIndex)
                    .ToList();
            if (selectedRowIndices.Count == 0) 
            {
                MessageBox.Show("未选择任何单元格!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            List<int> realIndexList = new List<int>();
            for (int i = 0; i < selectedRowIndices.Count; i++)
            {
                int realIndex = (int)dList[selectedRowIndices[i]];

                int j = 0;
                foreach (var item in _currentDocument.Columns.Values)
                {
                    if (j == 0 && item.Count == 0)
                    {
                        ((List<string>)item).Capacity--;
                        j++;
                        continue;
                    }
                    item.RemoveAt(realIndex);
                    j++;
                }
                realIndexList.Add(realIndex);
                dList.RemoveAt(selectedRowIndices[i]);
            }
            for (int i = selectedRowIndices[selectedRowIndices.Count-1]; i < dList.Count; i++)
            {
                //真实index索引
                uint dlItem = dList[i];
                //计算当前真实index大于删除行号数量
                int count = realIndexList.Count(p => p <= dlItem);
                dList[i] -= (uint)count;
            }
            // 2. 刷新界面（利用先赋0、后赋新值、挂起布局的优化组合拳）
            dataGridView1.SuspendLayout();
            dataGridView1.RowCount = 0;
            dataGridView1.RowCount = dList.Count;
            dataGridView1.ResumeLayout();

            dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex1].Cells[colIndex1];

            statusLabel.rowsCount = dList.Count;
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param> 增加行数
        /// <param name="addORremove"></param> 0:向上添加 1:向下添加
        private void datagridview_addRow(int count,int addORremove)
        {
            if (_currentDocument == null) return;
            var colIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            if (rowIndex < 0)
            {
                MessageBox.Show("未选择任何单元格!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int realRowIndex = (int)dList[rowIndex] + 1;
            for (int i = 0; i < count; i++)
            {
                //每列数据插入数据
                int j = 0;
                foreach (var item in _currentDocument.Columns.Values)
                {
                    if (j == 0)
                    {
                        if (item.Count==0)
                        {
                            ((List<string>)item).Capacity++;
                        }
                        else
                        {
                            ((List<string>)item).Insert(realRowIndex, "");
                        }
                    }
                    else if (j == 1)
                    {
                        ((List<uint>)item).Insert(realRowIndex, 0);
                    }
                    else
                    {
                        switch (_currentDocument.Fields[j - 2].FieldType)
                        {
                            case DntFieldType.Text:
                                ((List<string>)item).Insert(realRowIndex, "");
                                break;
                            case DntFieldType.BooleanInt:
                                ((List<int>)item).Insert(realRowIndex, 0);
                                break;
                            case DntFieldType.Int32:
                                ((List<int>)item).Insert(realRowIndex, 0);
                                break;
                            case DntFieldType.Percentage:
                            case DntFieldType.Float:
                                ((List<float>)item).Insert(realRowIndex, 0);
                                break;
                            default:
                                ((List<string>)item).Insert(realRowIndex, "");
                                break;
                        }
                    }
                    j++;
                }
                dList.Insert(rowIndex + addORremove, dList[rowIndex] + (uint)(count - i));
                _currentDocument.RecordCount++;
            }

            for (int k = rowIndex + addORremove + count; k < dList.Count; k++) 
            {
                dList[k] = dList[k] + (uint)count;
            }

            // 2. 刷新界面（利用先赋0、后赋新值、挂起布局的优化组合拳）
            dataGridView1.SuspendLayout();
            dataGridView1.RowCount = 0;
            dataGridView1.RowCount = dList.Count;
            dataGridView1.ResumeLayout();

            statusLabel.rowsCount = dList.Count;
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();

            dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex ].Cells[colIndex];
        }
        private void 向下插入一行insToolStripMenuItem_Click(object sender, EventArgs e)
        {
            datagridview_addRow(1, 1);
        }

        private void 向上插入一行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            datagridview_addRow(1, 0);
        }

        private void 删除当前行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            datagridview_removeRow();
        }

        private void 向下插入N行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 弹出输入框
            string input = Interaction.InputBox("请输入一个正整数：", "输入提示", "");

            if (string.IsNullOrEmpty(input))
            {
                // 用户点击了取消或未输入
                return;
            }

            // 校验是否为正整数
            if (int.TryParse(input, out int result) && result > 0)
            {
                //MessageBox.Show($"输入成功，数字为: {result}");
                datagridview_addRow(result, 1);
            }
            else
            {
                MessageBox.Show("输入无效，请输入大于0的正整数！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void 快捷参数窗口ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (quickParamForm == null || quickParamForm.IsDisposed)
            {
                quickParamForm = new QuickParamForm(this);

                // 核心设置：不在 Windows 任务栏中显示此窗口
                quickParamForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                quickParamForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                quickParamForm.Show(this);
            }
            else
            {
                quickParamForm.Activate();
            }
        }

        private void panel3_SizeChanged(object sender, EventArgs e)
        {
            // 2. 获取当前最新的尺寸
            Size currentSize = panel3.Size;

            // 3. 计算宽度和高度的变化差值 (新值 - 旧值)
            int deltaWidth = currentSize.Width - lastSize.Width;

            this.Width += deltaWidth;

            lastSize = currentSize;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 切换 Pane2 的显示和隐藏状态
            panel3.Visible = !panel3.Visible;
            if (panel3.Visible)
            {
                this.Width += panel3.Width;
            }
            else
            {
                this.Width -= panel3.Width;
            }
        }
        public void InsertValueToSelectedCell(string value)
        {
            if (dataGridView1.SelectedCells.Count==0) return;
            try
            {
                foreach (var cell in dataGridView1.SelectedCells.Cast<DataGridViewCell>())
                {
                    int realRowIndex = (int)dList[cell.RowIndex];
                    int colIndex = cell.ColumnIndex;
                    DntFieldDescription field = _currentDocument.GetFieldAt(colIndex);
                    var colList = _currentDocument.Columns[field.FieldName];

                    switch (field.FieldType)
                    {
                        case DntFieldType.Text:
                            ((List<string>)colList)[realRowIndex] = value.Trim();
                            break;
                        case DntFieldType.BooleanInt:
                        case DntFieldType.Int32:
                            ((List<int>)colList)[realRowIndex] = int.Parse(value.Trim());
                            break;
                        case DntFieldType.Percentage:
                        case DntFieldType.Float:
                            ((List<float>)colList)[realRowIndex] = float.Parse(value.Trim());
                            break;
                        default:
                            ((List<string>)colList)[realRowIndex] = value.Trim();
                            break;
                    }
                }
                dataGridView1.Refresh();
            }
            catch
            {
                MessageBox.Show("插入数据和单元格格式不符,请重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (isUpdateCellSelect) return;
            statusLabel.sumCount = 0;
            foreach (var cell in dataGridView1.SelectedCells.Cast<DataGridViewCell>())
            {
                if (double.TryParse(cell.Value?.ToString(), out double result))
                {
                    statusLabel.sumCount += result;
                }
                else
                {
                    statusLabel.sumCount = 0;
                    break;
                }
            }
            this.toolStripStatusLabel1.Text = statusLabel.mainStatusLabel();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            isUpdateCellSelect = true;
        }

        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            isUpdateCellSelect = false;
            dataGridView1_SelectionChanged(sender,e);
        }

        private void 计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. 如果窗口已经存在且未被销毁，先关闭并释放它
            if (calculatorForm != null && !calculatorForm.IsDisposed)
            {
                calculatorForm.Close(); // Close 会自动触发 Dispose
            }

            // 2. 无论之前是否存在，这里都必定重新构造
            calculatorForm = new CalculatorForm(this);

            // 核心设置：不在 Windows 任务栏中显示此窗口
            calculatorForm.ShowInTaskbar = false;

            // 核心设置：将起始位置设置为居中于父窗体（CenterParent）
            calculatorForm.StartPosition = FormStartPosition.CenterParent;

            // 让子窗口作为主窗口的附属并显示
            calculatorForm.Show(this);
        }

        private void dNT文件转换ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dntTranConverForm == null || dntTranConverForm.IsDisposed)
            {
                dntTranConverForm = new DntTranConverForm();

                // 核心设置：不在 Windows 任务栏中显示此窗口
                dntTranConverForm.ShowInTaskbar = false;
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                dntTranConverForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                dntTranConverForm.Show();
            }
            else
            {
                dntTranConverForm.Activate();
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // --- 1. 监听单键 (F1, F2, F3, F4, Insert, Delete) ---
            switch (e.KeyCode)
            {
                case Keys.F1://翻译列
                    //MessageBox.Show("触发了 F1 键");
                    button1_Click(sender, e);
                    e.Handled = true; // 阻止事件继续向下传递（可选）
                    return;
                case Keys.F2://单元格翻译
                    checkBox3.Checked = !checkBox3.Checked;
                    //MessageBox.Show("触发了 F2 键");
                    e.Handled = true;
                    return;
                case Keys.F3://翻译覆盖所选列
                    button4_Click(sender, e);
                    //MessageBox.Show("触发了 F3 键");
                    e.Handled = true;
                    return;
                case Keys.F4://翻译标题行
                    checkBox2.Checked = !checkBox2.Checked;
                    //MessageBox.Show("触发了 F4 键");
                    e.Handled = true;
                    return;
                case Keys.Insert://向下插入一行
                    //MessageBox.Show("触发了 Insert 键");
                    向下插入一行insToolStripMenuItem_Click(sender, e);
                    e.Handled = true;
                    return;
                case Keys.Delete://删除当前行
                    //MessageBox.Show("触发了 Delete 键");
                    删除当前行ToolStripMenuItem_Click(sender, e);
                    e.Handled = true;
                    return;
            }

            // --- 2. 监听组合键 (Ctrl + S) ---
            if (e.Control && e.KeyCode == Keys.S)
            {
                //MessageBox.Show("触发了 Ctrl + S (保存)");
                快速保存ctrlsToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }

            // --- 3. 监听组合键 (Ctrl + 1 到 7) ---
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.D1:
                        使用uistring源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D2:
                        使用物品源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D3:
                        使用技能源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D4:
                        使用地图源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D5:
                        使用怪物源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D6:
                        使用npc源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D7:
                        使用其他源ToolStripMenuItem_Click(sender, e);
                        e.Handled = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void 增加列ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (addColumnForm == null || addColumnForm.IsDisposed)
            {
                addColumnForm = new AddColumnForm(this);

                addColumnForm.ShowInTaskbar = false;

                addColumnForm.Show(this);
            }
            else
            {
                addColumnForm.Activate();
            }

            /***
            InitVirtualDataGridView(_currentDocument);

            statusLabel.rowsCount = (int)_currentDocument.RecordCount + 1;
            statusLabel.columnCount = _currentDocument.FieldCount + 1;
            ***/
        }

        private void 删除列ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentDocument == null) return;
            int colIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            if (colIndex < 2)
            {
                MessageBox.Show("无法删除第一列或第二列", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DntFieldDescription colField = _currentDocument.GetFieldAt(colIndex);
            // 1. 弹出询问对话框，设置标题、按钮（YesNoCancel）和警告图标
            DialogResult result = MessageBox.Show(
                $"是否删除列({colField.FieldName})？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {

                _currentDocument.Fields.RemoveAt(colIndex-2);
                _currentDocument.FieldCount = (ushort)_currentDocument.Fields.Count;

                InitVirtualDataGridView(_currentDocument);
                //重新恢复焦点
                dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[colIndex];
                this.dataGridView1.Invalidate();

                statusLabel.rowsCount = (int)_currentDocument.RecordCount + 1;
                statusLabel.columnCount = _currentDocument.FieldCount + 1;

                this.statusStrip1.Text = statusLabel.mainStatusLabel();
                MessageBox.Show($"删除列({colField.FieldName}成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
            
        }

        private void 清空DNT表格ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentDocument == null) return;

            DialogResult result = MessageBox.Show(
                "确定要清空表格的所有内容吗??？", // 提示信息
                "提示",                                        // 对话框标题
                MessageBoxButtons.YesNo,                // 显示 是、否、取消 三个按钮
                MessageBoxIcon.Question                       // 显示 问号 图标
            );

            // 2. 根据用户的点击结果，执行不同的业务逻辑
            if (result == DialogResult.Yes)
            {
                _currentDocument.Columns[_currentDocument.GetFieldAt(0).FieldName] = new List<string>(1);
                _currentDocument.Columns[_currentDocument.GetFieldAt(1).FieldName] = Enumerable.Repeat((uint)0, 1).ToList();
                foreach (var filed in _currentDocument.Fields)
                {
                    System.Collections.IList columnList;

                    switch (filed.FieldType)
                    {
                        case DntFieldType.Text:
                            columnList = Enumerable.Repeat(string.Empty, 1).ToList();
                            break;
                        case DntFieldType.BooleanInt:
                            columnList = Enumerable.Repeat(0, 1).ToList();
                            break;
                        case DntFieldType.Int32:
                            columnList = Enumerable.Repeat(0, 1).ToList();
                            break;
                        case DntFieldType.Percentage:
                        case DntFieldType.Float:
                            columnList = Enumerable.Repeat(0f, 1).ToList();
                            break;
                        default:
                            columnList = Enumerable.Repeat(string.Empty, 1).ToList();
                            break;
                    }
                    _currentDocument.Columns[filed.FieldName] = columnList;
                }
                dList = new List<uint> { 0 };
                _currentDocument.RecordCount = (uint)dList.Count;
                // 2. 刷新界面（利用先赋0、后赋新值、挂起布局的优化组合拳）
                dataGridView1.SuspendLayout();
                dataGridView1.RowCount = 0;
                dataGridView1.RowCount = dList.Count;
                dataGridView1.ResumeLayout();
                
                statusLabel.rowsCount = (int)_currentDocument.RecordCount + 1;
                statusLabel.columnCount = _currentDocument.FieldCount + 1;

                this.statusStrip1.Text = statusLabel.mainStatusLabel();

                MessageBox.Show("清空表格成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                // 用户点击了 “否”：不保存，直接跳过（如果是在关闭窗体，则允许直接关闭）
                // 这里通常留空，或者执行后续不需要保存的刷新操作
            }
        }
    }
}
