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
    }
}
