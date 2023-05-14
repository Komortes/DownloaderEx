using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderEx
{
    public static class Utilities
    {
        public static int CountOccurence(string input, char c)
        {
            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (c == input[i])
                {
                  count++;
                }   
            }
            return count;
        }
    }
}
