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

namespace DntEditor_Hang
{
    public partial class DntTranConverForm : Form
    {
        public DntTranConverForm()
        {
            InitializeComponent();
        }

        private void DntTranConverForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int fileCount = 0;
            int successCount = 0;
            if (files.Length > 0)
            {
                string filePath = files[0];
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    // 获取路径的属性
                    FileAttributes attr = File.GetAttributes(filePath);

                    // 判断属性中是否包含 Directory 标记
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (radioButton1.Checked==true)
                        {
                            //Console.WriteLine("这是一个【文件夹】");
                            string[] dntFiles = Directory.GetFiles(filePath, "*.dnt", SearchOption.TopDirectoryOnly);
                            fileCount = dntFiles.Length;
                            successCount = 0;
                            foreach (var item in dntFiles)
                            {
                                string parentPath = Path.GetDirectoryName(Path.GetDirectoryName(item)) + "\\" + Path.GetFileName(filePath) + "_csv";
                                //DNT转CSV函数
                                if (DntConver(item, parentPath))
                                    successCount++;
                            }
                        }
                        else if (radioButton2.Checked == true)
                        {
                            //Console.WriteLine("这是一个【文件夹】");
                            string[] dntFiles = Directory.GetFiles(filePath, "*.csv", SearchOption.TopDirectoryOnly);
                            fileCount = dntFiles.Length;
                            successCount = 0;
                            foreach (var item in dntFiles)
                            {
                                string parentPath = Path.GetDirectoryName(Path.GetDirectoryName(item)) + "\\" + Path.GetFileName(filePath) + "_dnt";
                                //CSV转DNT函数
                                if (CsvConver(item, parentPath))
                                    successCount++;
                            }
                        }
                        
                    }
                    else
                    {
                        fileCount = 1;
                        //Console.WriteLine("这是一个【文件】");
                        if (radioButton1.Checked == true)
                        {
                            string parentPath = Path.GetDirectoryName(filePath);
                            //DNT转CSV函数
                            if (DntConver(filePath, parentPath))
                                successCount++;
                        }
                        else if (radioButton2.Checked == true)
                        {
                            string parentPath = Path.GetDirectoryName(filePath);
                            //CSV转DNT函数
                            if(CsvConver(filePath, parentPath))
                                successCount++;
                        }
                    }
                    MessageBox.Show(this, $"转换文件{successCount}/{fileCount}！", "成功");
                }
                else
                {
                    MessageBox.Show(this, "路径不存在！", "格式错误");
                }

                
            }
        }
        private bool DntConver(string filePath,string savePath)
        {
            try
            {
                string fileName = Path.ChangeExtension(Path.GetFileName(filePath), ".csv");
                //先将dnt文件转换成DntDocument类
                DntDocument doc = DntConvertHelpers.LoadFromFile(filePath);
                //通过DntDocment类转换成CSV文件类型
                DntConvertHelpers.SaveToCsv(doc, savePath + "\\" + fileName);
                return true;
            }
            catch
            {
                return false;
            }
            return false;
        }
        private bool CsvConver(string filePath, string savePath)
        {
            try
            {
                string fileName = Path.ChangeExtension(Path.GetFileName(filePath), ".dnt");
                //先将CSV文件转换成DntDocument类
                DntDocument doc = DntConvertHelpers.LoadFromCsv(filePath);
                //通过DntDocment类转换成DNT文件类型
                DntConvertHelpers.SaveToFile(doc, savePath + "\\" + fileName);
                return true;
            }
            catch
            {
                return false;
            }
            
            return false;
        }
        private void DntTranConverForm_Load(object sender, EventArgs e)
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

        private void DntTranConverForm_DragEnter(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否包含“文件”（FileDrop）
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 如果是文件，将鼠标指针改为“复制”状态（显示小加号）
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                // 如果不是文件（比如拖的是一段文字），则不接受
                e.Effect = DragDropEffects.None;
            }
        }
    }
}
