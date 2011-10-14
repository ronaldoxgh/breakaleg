using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Breakaleg.Models;
using Breakaleg.Compilers;

namespace Breakaleg.Consoles
{
    class Program
    {
        static void Main(string[] args)
        {
            CodePiece c = null;
            var p = new BreakalegCompiler();
            
            Thread.Sleep(1000);

            var h0 = DateTime.Now;
            for (int i = 0; i < 90; i++)
                c = p.Parse("1+1");
            var h1 = DateTime.Now;
            Console.WriteLine("compiled 90 times in: " + (int)((h1 - h0).TotalMilliseconds) + "ms");

            var ctx = new Context();
            var hh0 = DateTime.Now;
            for (int i = 0; i < 50000; i++)
                c.Run(ctx);
            var hh1 = DateTime.Now;
            Console.WriteLine("ran 50000 times in: " + (int)((hh1 - hh0).TotalMilliseconds) + "ms");

            Console.ReadLine();
        }
    }
}
