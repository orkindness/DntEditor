using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DntEditor_Hang.Forms;

namespace DntEditor_Hang
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string filePath = string.Empty;
            int index = 0;
            // 2. 检查是否有参数传入 (假设参数格式为: MyProg.exe "D:\test.txt" 15)
            if (args != null && args.Length >= 2)
            {
                filePath = args[0]; // 第一个参数作为文件路径

                // 第二个参数尝试转为 int 类型的行数
                int.TryParse(args[1], out index);
            }
            else if (args != null && args.Length == 1)
            {
                // 如果只拖入了一个文件，没有传行数，则只给 filePath 赋值，index 保持默认值 0
                filePath = args[0];
            }
            Application.Run(new MainForm(filePath, index));
        }
    }
}
