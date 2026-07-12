using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DntEditor_Hang.Models
{
    //筛选控件类--通过此类规范化生成筛选条件控件
    class FilterParamCombo
    {
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
        public System.Windows.Forms.ComboBox ColumnComboBox;
        public System.Windows.Forms.ComboBox OperatorComboBox;
        public System.Windows.Forms.TextBox equlTextBox;
        public System.Windows.Forms.TextBox textBox;
        public System.Windows.Forms.TextBox MaxTextBox;
        public System.Windows.Forms.Button cancelBt;

        //等于列表下拉框
        public CheckedListBox checkedListBox;
        public ToolStripControlHost host;
        public ToolStripDropDown dropDown;

        public object[] operatorList1 = new object[] {
            "=",">",">=","<=","<","<>",
            "between"};
        public object[] operatorList2 = new object[] {
            "等于","like"};
        //与前一个筛选条件的计算关系
        public System.Windows.Forms.ComboBox ParamOperatorComboBox;

        public FilterParamCombo()
        {
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.ColumnComboBox = new System.Windows.Forms.ComboBox();
            this.OperatorComboBox = new System.Windows.Forms.ComboBox();
            this.equlTextBox = new System.Windows.Forms.TextBox();
            this.textBox = new System.Windows.Forms.TextBox();
            this.MaxTextBox = new System.Windows.Forms.TextBox();
            this.cancelBt = new System.Windows.Forms.Button();


            this.ParamOperatorComboBox = new System.Windows.Forms.ComboBox();

            this.flowLayoutPanel.SuspendLayout();
            // 
            // flowLayoutPanel1 流式布局画布初始化
            // 
            this.flowLayoutPanel.Controls.Add(this.ColumnComboBox);
            this.flowLayoutPanel.Controls.Add(this.OperatorComboBox);
            this.flowLayoutPanel.Controls.Add(this.equlTextBox);
            this.flowLayoutPanel.Controls.Add(this.textBox);
            this.flowLayoutPanel.Controls.Add(this.MaxTextBox);
            this.flowLayoutPanel.Controls.Add(this.cancelBt);
            this.flowLayoutPanel.Location = new System.Drawing.Point(3, 12);//坐标,等下需要注释掉
            this.flowLayoutPanel.Name = "flowLayoutPanel";                  //name需不需要注释掉,避免动态部署的时候发生冲突
            this.flowLayoutPanel.Size = new System.Drawing.Size(600, 33);
            this.flowLayoutPanel.TabIndex = 0;

            // 
            // ColumnComboBox
            // 
            this.ColumnComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ColumnComboBox.FormattingEnabled = true;
            this.ColumnComboBox.Location = new System.Drawing.Point(3, 3);
            this.ColumnComboBox.Name = "ColumnComboBox";
            this.ColumnComboBox.Size = new System.Drawing.Size(148, 26);
            this.ColumnComboBox.TabIndex = 0;
            // 
            // OperatorComboBox
            // 
            this.OperatorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OperatorComboBox.FormattingEnabled = true;
            this.OperatorComboBox.Location = new System.Drawing.Point(157, 3);
            this.OperatorComboBox.Name = "OperatorComboBox";
            this.OperatorComboBox.Size = new System.Drawing.Size(67, 26);
            this.OperatorComboBox.TabIndex = 1;
            this.OperatorComboBox.Items.AddRange(operatorList1);
            this.OperatorComboBox.SelectedIndex = 0;
            // 
            // equlTextBox
            // 
            this.equlTextBox.Location = new System.Drawing.Point(230, 3);
            this.equlTextBox.Name = "textBox5";
            this.equlTextBox.Size = new System.Drawing.Size(142, 28);
            this.equlTextBox.TabIndex = 6;
            //this.equlTextBox.TextChanged += new System.EventHandler(this.textBox5_TextChanged);
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(230, 3);//408
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(142, 28);
            this.textBox.TabIndex = 3;
            // 
            // MaxTextBox
            // 
            this.MaxTextBox.Location = new System.Drawing.Point(378, 3);//556
            this.MaxTextBox.Name = "MaxTextBox";
            this.MaxTextBox.Size = new System.Drawing.Size(142, 28);
            this.MaxTextBox.TabIndex = 4;
            // 
            // cancelBt
            // 
            this.cancelBt.Location = new System.Drawing.Point(526, 3);
            this.cancelBt.Name = "cancelBt";
            this.cancelBt.Size = new System.Drawing.Size(65, 30);
            this.cancelBt.TabIndex = 5;
            this.cancelBt.Text = "取消";
            this.cancelBt.UseVisualStyleBackColor = true;

            // 
            // ParamOperatorComboBox
            // 
            this.ParamOperatorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ParamOperatorComboBox.FormattingEnabled = true;
            this.ParamOperatorComboBox.Items.AddRange(new object[] {
            "AND",
            "OR"});
            this.ParamOperatorComboBox.Location = new System.Drawing.Point(315, -20);
            this.ParamOperatorComboBox.SelectedIndex = 0;
            this.ParamOperatorComboBox.Name = "comboBox4";
            this.ParamOperatorComboBox.Size = new System.Drawing.Size(90, 26);
            this.ParamOperatorComboBox.TabIndex = 1;

            //等于时的下拉框
            // 2. 初始化 CheckedListBox 和 Host
            checkedListBox = new CheckedListBox();
            //checkedListBox.Size = new Size(400, 250);
            checkedListBox.CheckOnClick = true; // 点击文字直接勾选
            checkedListBox.IntegralHeight = false; // 必须为 false
            // 【关键修改】：让 CheckedListBox 自动填满它的宿主容器 Host
            checkedListBox.Dock = DockStyle.Fill;
            // 【关键修改】：显式开启滚动条（确保超出范围时自动显示滚轮）
            checkedListBox.ScrollAlwaysVisible = true; // 或者设为 true 强行显示
            checkedListBox.HorizontalScrollbar = true;


            host = new ToolStripControlHost(checkedListBox);
            host.Padding = Padding.Empty;
            host.Margin = Padding.Empty;
            // 【关键修改】：让 Host 容器也填满外层的 DropDown
            host.Dock = DockStyle.Fill;
            // 1. 【最核心】允许这个 Host 容器接收焦点。
            // 如果不设为 true，即使调用 checkedListBox.Focus() 也会被操作系统拒绝。
            host.Available = true;

            // 3. 初始化下拉容器
            dropDown = new ToolStripDropDown();
            dropDown.Padding = Padding.Empty;
            // 关闭自动大小，否则它会被内部的控件撑大到屏幕边缘
            dropDown.AutoSize = false;
            // 强制设置弹出菜单的大小（必须大于或等于 checkedListBox 的 Size）
            dropDown.Size = new Size(415, 255);
            // 允许下拉窗体在显示时激活自身，从而能够捕获鼠标行为
            dropDown.AllowDrop = true;
            // 【重要】不要让下拉框自动捕获鼠标独占。
            // 这样可以确保鼠标悬停在上面时，Windows 能正确识别到鼠标正处于该控件的坐标内。
            dropDown.Capture = false;
            dropDown.Items.Add(host);

        }

    }
}
