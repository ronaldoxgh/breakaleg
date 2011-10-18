using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Breakaleg.Core.Compilers;
using Breakaleg.Core.Models;
using System.IO;

namespace Breakaleg.Consoles
{
    public class JSNamespace
    {
        public object window = new JSWindow();
        public object document = new JSDocument();
        public object @this;

        public JSNamespace()
        {
            @this = window;
        }

        public class Date
        {
            private DateTime value;
            public Date()
            {
                this.value = DateTime.Now;
            }

            public dynamic getTime()
            {
                return this;
            }

            public static int operator -(Date d1, Date d2)
            {
                return (int)(d1.value - d2.value).TotalMilliseconds;
            }
        }

        public class Array
        {
            public Array() { }
            public Array(params dynamic[] len) { }
        }

        public class Object { }

        public class String
        {
            public String() { }
            public String(dynamic arg) { }

            public static string Unboxing(dynamic arg)
            {
                return arg.ToString();
            }
        }

        public class Math
        {
            public static dynamic PI { get { return System.Math.PI; } }
            public static dynamic sqrt(dynamic p) { return System.Math.Sqrt(p); }
            public static dynamic cos(dynamic p) { return System.Math.Cos(p); }
            public static dynamic sin(dynamic p) { return System.Math.Sin(p); }
            public static dynamic abs(dynamic p) { return System.Math.Abs(p); }
            public static dynamic round(dynamic p) { return System.Math.Round((double)p); }
        }
    }

    public class JSWindow
    {
        public void setTimeout(dynamic proc, dynamic delay) { }

        public static void alert(dynamic msg)
        {
            Console.WriteLine("==============");
            Console.WriteLine(msg);
            Console.WriteLine("==============");
        }
    }

    public class HTMLDiv
    {
        private string _innerHTML;
        public string innerHTML
        {
            get { return _innerHTML; }
            set
            {
                /*if (_innerHTML == null)
                    Console.Write(value);
                else if (value.IndexOf(_innerHTML) == 0)
                    Console.Write(value.Substring(_innerHTML.Length));*/
                _innerHTML = value;
                Console.Write(value);
            }
        }
    }

    public class JSDocument
    {
        private Dictionary<string, object> elems = new Dictionary<string, object>();

        public JSDocument()
        {
            elems.Add("console", new HTMLDiv());
        }

        public dynamic getElementById(dynamic id)
        {
            object value;
            if (elems.TryGetValue(id, out value))
                return value;
            return null;
        }

        public void write(dynamic value)
        {
            var sb = new StringBuilder();
            var s = (string)value;
            var i = 0;
            while (i < s.Length)
            {
                i = s.IndexOf("<script", i, StringComparison.OrdinalIgnoreCase);
                if (i == -1) break;
                i = s.IndexOf(">", i + 1) + 1;
                var p = s.IndexOf("</script", i, StringComparison.OrdinalIgnoreCase);
                var code = s.Substring(i, p - i);
                sb.AppendLine(code);
                i = s.IndexOf(">", p + 1) + 1;
            }

            File.AppendAllText(@"c:\temp\jsns.txt", sb.ToString());///x

            ///Console.Write(sb.ToString());///x

            var t = new JSCompiler().Parse(sb.ToString());
            var cx = new NameContext();
            cx.UseNS(new JSNamespace());
            cx.UseNS(new JSWindow());
            t.Run(cx);
        }
    }

}
