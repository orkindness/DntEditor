using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Forms
{
    public partial class MainForm : Form
    {
        private SearchForm searchForm = null;
        private SetingForm setingForm = null;
        private BatchCryptoForm batchCryptoForm = null;
        public MainForm()
        {
            InitializeComponent();
        }

        private void 打开DNT目录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换 Pane2 的显示和隐藏状态
            panel3.Visible = !panel3.Visible;

            // 根据当前状态，动态修改按钮的文字提示（可选）
            if (panel3.Visible)
            {
                toolStripStatusLabel1.Text = "显示目录";
            }
            else
            {
                toolStripStatusLabel1.Text = "隐藏目录";
            }
        }

        private void 打开关闭工具栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 切换 Pane2 的显示和隐藏状态
            panel2.Visible = !panel2.Visible;

            // 根据当前状态，动态修改按钮的文字提示（可选）
            if (panel2.Visible)
            {
                toolStripStatusLabel1.Text = "显示目录";
            }
            else
            {
                toolStripStatusLabel1.Text = "隐藏目录";
            }
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
                setingForm = new SetingForm();

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
                // 2. 核心设置：将起始位置设置为居中于父窗体（CenterParent）
                batchCryptoForm.StartPosition = FormStartPosition.CenterParent;
                // 配合 this，让子窗口作为主窗口的附属，主窗口最小化时它也会跟着隐藏
                batchCryptoForm.Show(this);
            }
            else
            {
                batchCryptoForm.Activate();
            }
        }

    }
}
