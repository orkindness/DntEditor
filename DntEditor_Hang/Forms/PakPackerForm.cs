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
    public partial class PakPackerForm : Form
    {
        public PakPackerForm()
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void PakPackerForm_Load(object sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                int left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                int top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;

                this.Location = new Point(left, top);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string path = AppConfig.PakPath + "\\resource\\ext";
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("ext路径错误,请在设置中保存pak目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            string path = Path.GetDirectoryName(textBox4.Text.Trim());
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            string path = textBox2.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string path = textBox3.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            string path = textBox5.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                // 方式 1：直接打开并进入该文件夹
                Process.Start("explorer.exe", path);

                // 方式 2（进阶）：打开上级目录，并自动“高亮/选中”该文件夹
                // Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
