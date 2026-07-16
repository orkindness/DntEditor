using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DntEditor_Hang.Models
{
    public class DntDocument
    {
        // 【头部数据】
        public uint NullBytes { get; set; }       // dwNull (4字节)
        public ushort FieldCount { get; set; }    // wFieldNum (2字节)
        public uint RecordCount { get; set; }     // dwRecordNum (4字节)


        // 【字段描述块列表】（注意：Fields 里不需要包含 PKID，因为它在文件头后是固定的，保持原二进制描述即可）
        public List<DntFieldDescription> Fields { get; set; } = new List<DntFieldDescription>();

        // 【列式分布式存储核心】
        // 所有的列（包含 "PKID" 列和后面 Fields 定义的列）统一全部塞进这个字典里！
        public Dictionary<string, System.Collections.IList> Columns { get; set; }
            = new Dictionary<string, System.Collections.IList>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 辅助方法：快速根据列索引获取列的属性（0是中文翻译，1是PKID ,往后是动态列）
        /// </summary>
        public DntFieldDescription GetFieldAt(int columnIndex)
        {
            if (columnIndex <= 0 || columnIndex > Fields.Count) return null;
            if (columnIndex == 0)
            {
                return new DntFieldDescription
                {
                    FieldName = "ChineseTranslation",
                    FieldType = DntFieldType.Text
                };
            }
            else if(columnIndex == 1)
            {
                return new DntFieldDescription
                {
                    FieldName = "PKID",
                    FieldType = DntFieldType.Int32
                };
            }
            return Fields[columnIndex - 2];
        }

        /// <summary>
        /// 核心修改：初始化时，强制把 "PKID" 注册为第一列存储空间
        /// </summary>
        public void InitializeColumnStorage()
        {
            Columns.Clear();

            // 1. 【核心更改】强制注册 PKID 为一列独立的 uint 或 int 强类型空间区 ChineseTranslation
            Columns.Add("ChineseTranslation", new List<string>((int)RecordCount));
            Columns.Add("PKID", new List<uint>((int)RecordCount));
            // 2. 依次注册后面的动态列
            foreach (var field in Fields)
            {
                System.Collections.IList columnList;

                switch (field.FieldType)
                {
                    case DntFieldType.Text:
                        columnList = new List<string>((int)RecordCount);
                        break;
                    case DntFieldType.BooleanInt:
                        columnList = new List<int>((int)RecordCount);
                        break;
                    case DntFieldType.Int32:
                        columnList = new List<int>((int)RecordCount);
                        break;
                    case DntFieldType.Percentage:
                    case DntFieldType.Float:
                        columnList = new List<float>((int)RecordCount);
                        break;
                    default:
                        columnList = new List<string>((int)RecordCount);
                        break;
                }

                Columns.Add(field.FieldName, columnList);
            }
        }
    }

public class DntFieldDescription
    {
        public string FieldName { get; set; } = string.Empty;
        public DntFieldType FieldType { get; set; }
    }
}
