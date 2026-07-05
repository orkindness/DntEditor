using DntEditor_Hang.Models;
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
    public partial class FilterForm : Form
    {
        private List<uint> dList = null; 
        private DntDocument document = null;
        public FilterForm(List<uint> list, DntDocument doc)
        {
            InitializeComponent();
            this.MaximizeBox = false;
            dList = list;
            document = doc;
        }

        private void FilterForm_Load(object sender, EventArgs e)
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
    }
}
