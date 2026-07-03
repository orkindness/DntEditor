using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DntEditor_Hang.Helpers
{
    public static class ListSearch
    {
        public static int FindNext<T>(List<T> source,int startIndex,Func<T,bool> predicate)
        {
            if (source == null || source.Count == 0)
            {
                return -1;
            }
            startIndex = Math.Max(0, Math.Min(startIndex, source.Count - 1));

            for (int i = startIndex+1; i < source.Count; i++)
            {
                if (predicate(source[i]))
                    return i;
            }
            for (int i = 0; i < startIndex; i++)
            {
                if (predicate(source[i]))
                    return i;
            }
            return -1;
        }
    }
}
