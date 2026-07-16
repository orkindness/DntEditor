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
                        compareText(doc,dntFiles[count]);
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
        private void compareText(DntDocument doc,string filePath)
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
            if (e.ColumnIndex==0)
            {
                e.Value = dataList[e.RowIndex].fileName;
            }
            else if (e.ColumnIndex == 1)
            {
                e.Value = dataList[e.RowIndex].rowIndex;
            }
            else if (e.ColumnIndex == 2)
            {
                e.Value = dataList[e.RowIndex].context;
            }
            
        }
    }
    public class DeepSearchItem
    {
        public string fileName { get; set; }
        public int rowIndex { get; set; }
        public string context { get; set; }
    }
}
