﻿using System;
using System.Threading;
using System.IO;
using Breakaleg.Core.Compiler;
using Breakaleg.Core.Dynamic;
using Breakaleg.Core.Models;

namespace Breakaleg.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            TestSunSpider();
        }

        static void TestSunSpider()
        {
            var contents = File.ReadAllText(@"c:\projetos\breakaleg\sunspider\sunspider-test-contents.js");
            var prefix = File.ReadAllText(@"c:\projetos\breakaleg\sunspider\sunspider-test-prefix.js");
            var run = File.ReadAllText(@"c:\projetos\breakaleg\sunspider\sunspider-test-run.js");

            var c = new JSCompiler();

            var t = c.Parse(contents + prefix + run);
            var cx = new JSNames();
            t.Run(cx);

            var tc = cx.GetField("testContents");
            var ts = cx.GetField("tests");
            var ca = cx.GetField("categories");

            System.Console.Write(tc + " " + ts + " " + ca);
        }

        static void Clock()
        {
            CodePiece c = null;
            var p = new JSCompiler();

            Thread.Sleep(1000);

            var h0 = DateTime.Now;
            for (int i = 0; i < 90; i++)
                c = p.Parse("1+1");
            var h1 = DateTime.Now;
            System.Console.WriteLine("compiled 90 times in: " + (int)((h1 - h0).TotalMilliseconds) + "ms");

            var ctx = new NameContext();
            var hh0 = DateTime.Now;
            for (int i = 0; i < 50000; i++)
                c.Run(ctx);
            var hh1 = DateTime.Now;
            System.Console.WriteLine("ran 50000 times in: " + (int)((hh1 - hh0).TotalMilliseconds) + "ms");

            System.Console.ReadLine();
        }
    }
}
