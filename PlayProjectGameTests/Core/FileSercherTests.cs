using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileSerch;
using System.IO;
using System.Diagnostics;

namespace Tests
{
    [TestClass()]
    public class FileSercherTests
    {
        [TestMethod()]
        public void EnumerateFilesTest()
        {
            var sw = Stopwatch.StartNew();
            var files = (new DriveInfo(@"i:\")).EnumerateFiles().ToArray();
            var elapsed = sw.ElapsedMilliseconds.ToString();
            Console.WriteLine(string.Format("Found {0} files, elapsed {1} ms", files.Length, elapsed));
        }
    }
}