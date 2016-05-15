using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisParser
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            StellarisFileParser fp = new StellarisFileParser();

            RecursiveSearch(args[0], fp);
            fp.Merge();
            fp.Write("merged.txt");
        }

        static void RecursiveSearch(string dir, StellarisFileParser fp)
        {
            foreach (string s in System.IO.Directory.EnumerateDirectories(dir))
            {
                RecursiveSearch(s, fp);
            }

            foreach(string s in System.IO.Directory.EnumerateFiles(dir, "*.txt"))
            {
                fp.Load(s);
            }
        }
    }
}
