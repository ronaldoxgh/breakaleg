using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Breakaleg.Core.Compilers;
using Breakaleg.Core.Models;

namespace Breakaleg.Consoles
{
    public class JSNS
    {
        public object window = new JSWindow();
        public object document = new JSDocument();

        public class Date
        {
            public dynamic getTime()
            {
                return new Date();
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
        }
        public class Math
        {
            public static dynamic sqrt(dynamic p) { return 0; }
            public static dynamic cos(dynamic p) { return 0; }
            public static dynamic sin(dynamic p) { return 0; }
        }
    }

    class JSWindow
    {
        public void setTimeout(dynamic proc, dynamic delay) { }
    }

    class HTMLDiv
    {
        public string innerHTML;
    }

    class JSDocument
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

            var t = new BreakalegCompiler().Parse(sb.ToString());
            var cx = new Context();
            cx.AddNamespace(new JSNS());
            t.Run(cx);
        }
    }

}
