using DntEditor_Hang.Helpers;
using DntEditor_Hang.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class DntDeepSearchForm : Form
    {
        List<DntDocument> dntDocuments = null;
        string[] dntFiles = null;

        List<DeepSearchItem> dataList = null;
        public DntDeepSearchForm()
        {
            InitializeComponent();
            dataList = new List<DeepSearchItem>();
            //this.dataGridView1.RowCount = 0;

            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;
        }

        private void DntDeepSearchForm_Load(object sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                int left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                int top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;

                this.Location = new Point(left, top);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string searchText = textBox1.Text.Trim();
            string folderPath = AppConfig.DntPath;

            dataList = new List<DeepSearchItem>();

            if (string.IsNullOrEmpty(searchText)) return;
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //titleStatusLabel.stopwatch = new Stopwatch();
            //titleStatusLabel.stopwatch.Start();
            try
            {
                // 1. 改变鼠标状态为“等待/漏斗”状态
                Cursor.Current = Cursors.WaitCursor;
                this.toolStripStatusLabel1.Text = "正在拼命检索中......请稍后";
                // 【核心修复】：强行让状态栏所属的 statusStrip 控件（请根据你实际的控件名修改，通常是 statusStrip1）立即重绘更新界面
                // 如果你不知道状态栏叫什么名字，也可以直接写 this.Refresh(); 强行刷新整个窗体
                if (toolStripStatusLabel1.Owner != null)
                {
                    toolStripStatusLabel1.Owner.Refresh();
                }

                if (dntDocuments == null)
                {
                    dntFiles = Directory.GetFiles(folderPath, "*.dnt", SearchOption.TopDirectoryOnly);
                    dntDocuments = new List<DntDocument>();

                    foreach (string filePath in dntFiles)
                    {
                        DntDocument doc = DntConvertHelpers.LoadFromFile(filePath);
                        dntDocuments.Add(doc);
                        //调用对比方法
                        compareText(doc, filePath);
                    }
                }
                else
                {
                    int count = 0;
                    //调用对比方法
                    foreach (var doc in dntDocuments)
                    {
                        compareText(doc, dntFiles[count]);
                        count++;
                    }
                }
                if (dataList.Count != 0)
                {
                    dataGridView1.SuspendLayout();
                    this.dataGridView1.RowCount = dataList.Count;
                    dataGridView1.ResumeLayout();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"读取DNT文件发生错误: {ex.Message}", "读取失败");
            }
            finally
            {
                // 3. 无论成功或失败，停止秒表
                //titleStatusLabel.stopwatch.Stop();
                stopwatch.Stop();
                Cursor.Current = Cursors.Default;
            }

            string timeConsumed = "";
            if (stopwatch != null)
            {
                timeConsumed = $"深度查询耗时: {stopwatch.ElapsedMilliseconds} 毫秒";

                // 如果耗时较长，可以自动转换为秒（可选优化逻辑）
                if (stopwatch.Elapsed.TotalSeconds >= 1)
                {
                    timeConsumed = $"深度查询耗时: {stopwatch.Elapsed.TotalSeconds:F2} 秒"; // 保留两位小数
                }
            }

            this.toolStripStatusLabel1.Text = "匹配数:" + dataList.Count + " | " + timeConsumed;
        }
        private void compareText(DntDocument doc, string filePath)
        {
            string searchText = textBox1.Text.Trim();
            List<int> rowList = new List<int>();

            foreach (var list in doc.Columns.Values)
            {
                List<int> indexList = new List<int>();
                if (checkBox1.Checked)
                {//精确匹配
                    for (int i = 0; i < list.Count; i++)
                    {
                        var value = list[i];
                        if (value.ToString().Equals(searchText))
                        {
                            indexList.Add(i);
                            /***
                            indexList.Add(new DeepSearchItem { 
                            filePath = filePath,
                            fileName = Path.GetFileName(filePath),
                            rowIndex = i,
                            context = getContext(doc, i)
                            });
                            ***/
                        }
                    }
                }
                else
                {//包含匹配
                    for (int i = 0; i < list.Count; i++)
                    {
                        var value = list[i];
                        if (value.ToString().Contains(searchText))
                        {
                            indexList.Add(i);
                        }
                    }
                }
                rowList = rowList.Union(indexList).ToList();
            }
            foreach (var index in rowList)
            {
                dataList.Add(new DeepSearchItem
                {
                    filePath = filePath,
                    fileName = Path.GetFileName(filePath),
                    rowIndex = index,
                    context = getContext(doc, index)
                });
            }
        }

        private string getContext(DntDocument doc, int rowIndex)
        {
            string context = "";
            foreach (var item in doc.Columns.Values)
            {
                if (item.Count > 0)
                {
                    context += item[rowIndex].ToString() + ",";
                }
            }
            return context.Trim(",".ToCharArray());
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (dataGridView1.RowCount == 0) return;
            if (dataList == null || e.RowIndex < 0) return;
            if (e.ColumnIndex == 0)
            {
                e.Value = dataList[e.RowIndex].fileName;
            }
            else if (e.ColumnIndex == 1)
            {
                e.Value = dataList[e.RowIndex].rowIndex + 1;
            }
            else if (e.ColumnIndex == 2)
            {
                e.Value = dataList[e.RowIndex].context;
            }

        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 避坑检查：e.RowIndex >= 0 确保双击的是有效数据行
            // 如果 e.RowIndex == -1，说明用户双击的是“列标题（ColumnHeader）”，此时直接跳过
            if (e.RowIndex >= 0)
            {
                try
                {
                    // 场景示例：假设你的表格第一列（索引 0）存的是文件或文件夹的完整路径
                    //object cellValue = dataGridView1.Rows[e.RowIndex].Cells[0].Value;
                    object cellValue = dataList[e.RowIndex].filePath;

                    if (cellValue != null)
                    {
                        string targetPath = cellValue.ToString();

                        // 业务逻辑 1：如果双击的是个文件，用系统默认程序直接打开它（如双击效果）
                        if (File.Exists(targetPath))
                        {
                            // 1. 获取当前正在运行的这个 exe 的完整路径
                            string currentExePath = Application.ExecutablePath;

                            // 2. 准备启动参数：为了防止文件路径中带有空格导致解析错误，用双引号把路径包裹起来
                            // 格式形如： "C:\test file.txt" 5
                            string arguments = $"\"{targetPath}\" {dataList[e.RowIndex].rowIndex}";
                            // 3. 直接调用当前 exe 启动一个全新独立的进程，并传入文件路径
                            System.Diagnostics.Process.Start(currentExePath, arguments);
                        }
                        // 业务逻辑 2：如果双击的是个文件夹，直接打开该文件夹
                        else if (Directory.Exists(targetPath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", targetPath);
                        }
                        else
                        {
                            // 业务逻辑 3：如果只是普通文本，可以弹窗提示当前双击的信息
                            MessageBox.Show($"你双击了第 {e.RowIndex} 行，内容是: {targetPath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开目标路径: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
    public class DeepSearchItem
    {
        public string filePath { get; set; }
        public string fileName { get; set; }
        public int rowIndex { get; set; }
        public string context { get; set; }
    }
}
