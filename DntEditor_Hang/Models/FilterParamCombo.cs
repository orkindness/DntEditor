using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    //筛选控件类--通过此类规范化生成筛选条件控件
    class FilterParamCombo
    {
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
        private System.Windows.Forms.ComboBox ColumnComboBox;
        private System.Windows.Forms.ComboBox OperatorComboBox;
        private System.Windows.Forms.ComboBox DistinctComboBox;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.TextBox MaxTextBox;
        private System.Windows.Forms.Button cancelBt;

        //与前一个筛选条件的计算关系
        private System.Windows.Forms.ComboBox ParamOperatorComboBox;

        public FilterParamCombo()
        {
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.ColumnComboBox = new System.Windows.Forms.ComboBox();
            this.OperatorComboBox = new System.Windows.Forms.ComboBox();
            this.DistinctComboBox = new System.Windows.Forms.ComboBox();
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
            this.flowLayoutPanel.Controls.Add(this.DistinctComboBox);
            this.flowLayoutPanel.Controls.Add(this.textBox);
            this.flowLayoutPanel.Controls.Add(this.MaxTextBox);
            this.flowLayoutPanel.Controls.Add(this.cancelBt);
            this.flowLayoutPanel.Location = new System.Drawing.Point(3, 12);//坐标,等下需要注释掉
            this.flowLayoutPanel.Name = "flowLayoutPanel";                  //name需不需要注释掉,避免动态部署的时候发生冲突
            this.flowLayoutPanel.Size = new System.Drawing.Size(781, 33);
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
            // 
            // DistinctComboBox
            // 
            this.DistinctComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DistinctComboBox.FormattingEnabled = true;
            this.DistinctComboBox.Location = new System.Drawing.Point(230, 3);
            this.DistinctComboBox.Name = "DistinctComboBox";
            this.DistinctComboBox.Size = new System.Drawing.Size(172, 26);
            this.DistinctComboBox.TabIndex = 2;
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(408, 3);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(142, 28);
            this.textBox.TabIndex = 3;
            // 
            // MaxTextBox
            // 
            this.MaxTextBox.Location = new System.Drawing.Point(556, 3);
            this.MaxTextBox.Name = "MaxTextBox";
            this.MaxTextBox.Size = new System.Drawing.Size(142, 28);
            this.MaxTextBox.TabIndex = 4;
            // 
            // cancelBt
            // 
            this.cancelBt.Location = new System.Drawing.Point(704, 3);
            this.cancelBt.Name = "cancelBt";
            this.cancelBt.Size = new System.Drawing.Size(65, 30);
            this.cancelBt.TabIndex = 5;
            this.cancelBt.Text = "取消";
            this.cancelBt.UseVisualStyleBackColor = true;
        }

    }
}
