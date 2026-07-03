using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Models
{
    public class ColumTranslationItem
    {
        public bool isTrans { get; set; }
        public List<string> TranslatedTextList { get; set; }

        public ColumTranslationItem()
        {
            isTrans = false;
            TranslatedTextList = new List<string>();
        }
    }
}
