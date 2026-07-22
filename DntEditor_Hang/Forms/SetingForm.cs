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
    public partial class SetingForm : Form
    {
        private MainForm mainForm;
        public SetingForm(MainForm mForm)
        {
            mainForm = mForm;
            InitializeComponent();

            this.MaximizeBox = false;     // 隐藏/禁用最大化按钮
            this.MinimizeBox = false;     // 隐藏/禁用最小化按钮
            this.ControlBox = true;       // 确保关闭按钮可见

            // 从同目录下的 config.ini 读取数据（分为 "Paths" 和 "Settings" 两个小节）
            textBox1.Text = AppConfig.DntPath;
            textBox2.Text = AppConfig.PlainPath;
            textBox3.Text = AppConfig.CipherPath;
            textBox4.Text = AppConfig.PakPath;
            textBox5.Text = AppConfig.ClientPath;
            checkBox1.Checked = AppConfig.IsSyncSaveEnabled;
        }

        private void SetingForm_Load(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
        {
            string path = GlobalHelper.SelectFolder("请选择 DNT 目录", textBox1.Text.Trim());
            if (path != null) textBox1.Text = path;
            button2_Click(sender, e);
        }
        // DNT 目录 保存
        private void button2_Click(object sender, EventArgs e)
        {
            AppConfig.DntPath = textBox1.Text.Trim();
            AppConfig.Save();
        }
        /// <summary>
        /// 保存同时保存明文密文目录设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            AppConfig.IsSyncSaveEnabled = checkBox1.Checked;
            mainForm.checkBox1_CheckedChanged();
            //AppConfig.Save();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string path = GlobalHelper.SelectFolder("请选择 明文 目录", textBox2.Text.Trim());
            if (path != null) textBox2.Text = path;
            button3_Click(sender, e);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string path = GlobalHelper.SelectFolder("请选择 密文 目录", textBox3.Text.Trim());
            if (path != null) textBox3.Text = path;
            button5_Click(sender, e);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string path = GlobalHelper.SelectFolder("请选择 补丁 目录", textBox4.Text.Trim());
            if (path != null) textBox4.Text = path;
            button7_Click(sender, e);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string path = GlobalHelper.SelectFolder("请选择 客户端 目录", textBox5.Text.Trim());
            if (path != null) textBox5.Text = path;
            button10_Click(sender, e);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AppConfig.PlainPath = textBox2.Text.Trim();
            AppConfig.Save();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AppConfig.CipherPath = textBox3.Text.Trim();
            AppConfig.Save();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            AppConfig.PakPath = textBox4.Text.Trim();
            AppConfig.Save();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            AppConfig.ClientPath = textBox5.Text.Trim();
            AppConfig.Save();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(path))
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

        private void button12_Click(object sender, EventArgs e)
        {
            string path = textBox2.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(path))
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
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(path))
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
            string path = textBox4.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.GetDirectoryName(path);
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
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(path))
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

        //明文目录DNT文件转移至PAK目录
        private void button16_Click(object sender, EventArgs e)
        {
            string filePath = textBox2.Text.Trim();
            if (!string.IsNullOrEmpty(filePath) && !Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string pakPath = Path.Combine(textBox4.Text.Trim(), "resource", "ext");
            if (!string.IsNullOrEmpty(pakPath) && !Directory.Exists(pakPath))
            {
                Directory.CreateDirectory(pakPath);
            }
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(pakPath))
            {
                MessageBox.Show("目录未设置,请先正确设置目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] dntFiles = Directory.GetFiles(filePath, "*.dnt", SearchOption.TopDirectoryOnly);

            int fileCount = dntFiles.Length;
            int succFileCount = 0;
            foreach (var file in dntFiles)
            {
                string fileName = Path.GetFileName(file);
                string copyFilePath = Path.Combine(pakPath, fileName);
                try
                {
                    File.Copy(file, copyFilePath, true);
                    succFileCount++;
                }
                catch
                {
                    
                }
            }
            MessageBox.Show($"成功Copy明文目录中的DNT文件{succFileCount}/{fileCount}至PAK路径{pakPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void button20_Click(object sender, EventArgs e)
        {
            panel1.Visible = !panel1.Visible;
            if (panel1.Visible)
            {
                this.Width += panel1.Width;
            }
            else
            {
                this.Width -= panel1.Width;
            }
        }
        /// <summary>
        /// 密文目录DNT文件转移至PAK目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            string filePath = textBox3.Text.Trim();
            if (!string.IsNullOrEmpty(filePath) && !Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string pakPath = Path.Combine(textBox4.Text.Trim(), "resource", "ext");
            if (!string.IsNullOrEmpty(pakPath) && !Directory.Exists(pakPath))
            {
                Directory.CreateDirectory(pakPath);
            }
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(pakPath))
            {
                MessageBox.Show("目录未设置,请先正确设置目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string[] dntFiles = Directory.GetFiles(filePath, "*.dnt", SearchOption.TopDirectoryOnly);

            int fileCount = dntFiles.Length;
            int succFileCount = 0;
            foreach (var file in dntFiles)
            {
                string fileName = Path.GetFileName(file);
                string copyFilePath = Path.Combine(pakPath, fileName);
                try
                {
                    File.Copy(file, copyFilePath, true);
                    succFileCount++;
                }
                catch
                {

                }
            }
            MessageBox.Show($"成功Copy密文目录中的DNT文件{succFileCount}/{fileCount}至PAK路径{pakPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>
        /// PAK目录转移PAK文件至客户端路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button19_Click(object sender, EventArgs e)
        {
            string pakPath = textBox4.Text.Trim();
            string clientPath = textBox5.Text.Trim();

            if (string.IsNullOrEmpty(pakPath) || string.IsNullOrEmpty(clientPath))
            {
                MessageBox.Show("目录未设置,请先正确设置目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(pakPath) || !Directory.Exists(clientPath))
            {
                MessageBox.Show("目录未设置,请先正确设置目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            pakPath = Path.GetDirectoryName(pakPath);

            string[] dntFiles = Directory.GetFiles(pakPath, "*.pak", SearchOption.TopDirectoryOnly);
            int fileCount = dntFiles.Length;
            int succFileCount = 0;
            foreach (var file in dntFiles)
            {
                string fileName = Path.GetFileName(file);
                string copyFilePath = Path.Combine(clientPath, fileName);
                try
                {
                    File.Move(file, copyFilePath);
                    succFileCount++;
                }
                catch
                {

                }
            }
            MessageBox.Show($"成功转移PAK文件({succFileCount}/{fileCount})至客户端路径:{clientPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            try
            {
                string path = textBox4.Text.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string targetPath = path;

                // 确保是一个文件夹
                if (Directory.Exists(targetPath))
                {
                    // 1. 获取文件夹的名称 (例如 "Resource")
                    string folderName = Path.GetFileName(targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    // 2. 获取该文件夹的上级目录 
                    string parentDir = Path.GetDirectoryName(targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    // 3. 拼接出同名的 .PAK 文件输出路径 (例如 @"C:\Game\Resource.pak")
                    string outputPakPath = Path.Combine(parentDir, folderName + ".pak");

                    // 4. 执行打包
                    this.Text = "设置     正在打包中...";
                    PakPacker.PackFolderToPak(targetPath, outputPakPath);
                    this.Text = "设置     打包完成";

                    // 5. 打开指定路径（打开上级目录并定位到该PAK文件）
                    //System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPakPath}\"");

                    MessageBox.Show($"PAK文件打包成功！\n已生成至：{outputPakPath}");
                }
                else
                {
                    MessageBox.Show("路径错误,请在设置正确目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打包失败: {ex.Message}");
            }
        }
    }
}
