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
using DntEditor_Hang.Models;
using DntEditor_Hang.Helpers;

namespace DntEditor_Hang.Forms
{
    public partial class BatchCryptoForm : Form
    {
        public BatchCryptoForm()
        {
            InitializeComponent();

            this.MaximizeBox = false;     // 隐藏/禁用最大化按钮
            this.MinimizeBox = false;     // 隐藏/禁用最小化按钮
            this.ControlBox = true;       // 确保关闭按钮可见

            this.textBox1.Text = AppConfig.PlainPath;
            this.textBox2.Text = AppConfig.CipherPath;
        }

        private void BatchCryptoForm_Load(object sender, EventArgs e)
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
        /// 加密
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string dntPath = textBox1.Text.Trim().Trim('\\');
            string dntCyptoPath = textBox2.Text.Trim().Trim('\\');
            if (string.IsNullOrEmpty(dntPath) || string.IsNullOrEmpty(dntCyptoPath))
            {
                // 弹出警告提示框
                MessageBox.Show(
                    "密文或明文目录没有设置，请输入！", // 提示内容
                    "系统提示",                         // 弹窗标题
                    MessageBoxButtons.OK,               // 只显示“确定”按钮
                    MessageBoxIcon.Warning              // 显示黄色的“警告”感叹号图标 [1]
                );
            }
            // 3. 获取目标路径下所有的 .dnt 文件
            string[] dntFiles = Directory.GetFiles(dntPath, "*.dnt", SearchOption.TopDirectoryOnly);
            //string[] dntCyptoFiles = Directory.GetFiles(dntCyptoPath, "*.dnt", SearchOption.TopDirectoryOnly);
            int iTotal = dntFiles.Length;
            int count = 0;
            foreach (string file in dntFiles)
            {
                string fileName = Path.GetFileName(file);
                string outFile = dntCyptoPath + "\\" + fileName;
                if (CyclicXorCryptoHelper.ProcessFile(file, outFile, true))
                {
                    count++;
                }
            }
            // 弹出加密成功提示框
            MessageBox.Show(
                "文件加密成功！成功文件数量: ["+ count+"/"+ iTotal+"]",       // 提示内容
                "系统提示",            // 弹窗标题
                MessageBoxButtons.OK,   // 只显示“确定”按钮
                MessageBoxIcon.Information // 显示蓝色的“i”信息图标（表示操作成功）
            );

        }
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            string dntPath = textBox1.Text.Trim().Trim('\\');
            string dntCyptoPath = textBox2.Text.Trim().Trim('\\');
            if (string.IsNullOrEmpty(dntPath) || string.IsNullOrEmpty(dntCyptoPath))
            {
                // 弹出警告提示框
                MessageBox.Show(
                    "密文或明文目录没有设置，请输入！", // 提示内容
                    "系统提示",                         // 弹窗标题
                    MessageBoxButtons.OK,               // 只显示“确定”按钮
                    MessageBoxIcon.Warning              // 显示黄色的“警告”感叹号图标 [1]
                );
            }
            // 3. 获取目标路径下所有的 .dnt 文件
            //string[] dntFiles = Directory.GetFiles(dntPath, "*.dnt", SearchOption.TopDirectoryOnly);
            string[] dntCyptoFiles = Directory.GetFiles(dntCyptoPath, "*.dnt", SearchOption.TopDirectoryOnly);
            int iTotal = dntCyptoFiles.Length;
            int count = 0;
            foreach (string file in dntCyptoFiles)
            {
                string fileName = Path.GetFileName(file);
                string outFile = dntPath + "\\" + fileName;
                if (CyclicXorCryptoHelper.ProcessFile(file, outFile))
                {
                    count++;
                }
            }
            // 弹出加密成功提示框
            MessageBox.Show(
                "文件解密成功！成功文件数量: [" + count + "/" + iTotal + "]",       // 提示内容
                "系统提示",            // 弹窗标题
                MessageBoxButtons.OK,   // 只显示“确定”按钮
                MessageBoxIcon.Information // 显示蓝色的“i”信息图标（表示操作成功）
            );
        }
    }
}
