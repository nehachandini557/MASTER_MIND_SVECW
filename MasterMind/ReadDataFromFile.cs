using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MasterMind
{
    public class ReadDataFromFile
    {
        public int ReadWordsFromFile(string[] words, string filename)
        {
            //string filename = "output.txt";
            if (System.IO.File.Exists(filename) == false)
                return -1;
            System.IO.StreamReader s = new System.IO.StreamReader(filename);
            int count = 0;
            while (true)
            {
                if (s.EndOfStream == true)
                    break;
                words[count++] = s.ReadLine();
            }
            s.Close();
            return count;
        }

    }
}