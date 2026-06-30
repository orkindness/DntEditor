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
    public partial class IniTranslationForm1 : Form
    {
        public IniTranslationForm1()
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = true;

            this.textBox1.TextChanged += textBox1_TextChanged;
            this.textBox1.Text = AppConfig.DntPath;
            tipDisable();
        }
        private void tipDisable()
        {
            if (string.IsNullOrEmpty(this.textBox1.Text))
            {
                this.label3.Visible = true;
            }
            else
            {
                this.label3.Visible = false;
            }
        }
        //dnt目录如果为空则提示
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            tipDisable();
        }

        private void IniTranslationForm1_Load(object sender, EventArgs e)
        {
            if(this.Owner != null)
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
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string filePath = GlobalHelper.SelectFile("请选择uistring.xnl文件", "uistring ", "*.xml");
            if (filePath != null) textBox2.Text = filePath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!GlobalHelper.ConvertXmlToIni(textBox2.Text, GlobalHelper.AppRootPath + translationDicts.SourcePath+ translationDicts.UistringSource+".ini"))
            {
                MessageBox.Show(this, "创建失败", "错误");
            }
            // 可以配合 FolderBrowserDialog 让用户选，这里直接用假定路径演示///public const string SourcePath = "\\database\\";
        ///public const string UistringSource = "uistring.ini";
            string dntFolderPath = textBox1.Text.Trim();
            string uistringPath = GlobalHelper.AppRootPath + translationDicts.SourcePath + translationDicts.UistringSource+".ini";
            try
            {
                // 开启鼠标等待状态
                Cursor.Current = Cursors.WaitCursor;
                //checkBox1;物品;
                //checkBox2;技能;
                //checkBox3;地图;
                //checkBox4;怪物;
                //checkBox5;npc;
                List<string> Checks = new List<string>();
                if (checkBox1.Checked)
                {
                    Checks.Add(translationDicts.ItemSource);
                }
                if (checkBox2.Checked)
                {
                    Checks.Add(translationDicts.SkillSource);
                }
                if (checkBox3.Checked)
                {
                    Checks.Add(translationDicts.MapSource);
                }
                if (checkBox4.Checked)
                {
                    Checks.Add(translationDicts.MonsterSource);
                }
                if (checkBox5.Checked)
                {
                    Checks.Add(translationDicts.NPCSource);
                }
                // 一行代码，开启全自动化翻译、碰撞、分组、去重合并合并与导出流水线
                // 传入当前包含 FormatGameText 的类实例（比如你项目中的 translationDicts 实例）
                DntBatchTranslator.ExecuteBatchTranslation(dntFolderPath, uistringPath, Checks);

                Cursor.Current = Cursors.Default;
                MessageBox.Show("全自动化同类型批量翻译完成！.ini 文件已成功生成在对应目录下。", "批量成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show($"自动化执行中断: {ex.Message}", "强防御警报", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
