using System;
using System.Linq;
using System.Threading;
using Breakaleg.Core.Models;

namespace Breakaleg.Core.Dynamic
{
    public class JSNames : NameContext
    {
        public JSNames()
            : base()
        {
            var win = new Instance();
            win.ShareFieldsWith(this);
            SetField("this", win);
            SetField("window", win);
            SetField("Math", NewMathType());
            SetField("String", NewStringType());
            SetField("Array", NewArrayType());
            SetField("Date", NewDateType());
            SetField("Object", NewObjectType());
        }

        private static ThreadLocal<JSNames> running = new ThreadLocal<JSNames>();
        public static JSNames Running { get { return running.Value; } }

        public ExitResult Run(CodePiece code)
        {
            ExitResult result = null;
            running.Value = this;
            try
            {
                result = code.Run(this);
            }
            finally
            {
                running.Value = null;
            }
            return result;
        }

        private Instance NewMathType()
        {
            // math nao tem prototype
            var math = new Instance();
            math.SetField("PI", new Instance(Math.PI));
            math.SetMethod("sqrt", (s, a) => new Instance(Math.Sqrt((double)a[0].Scalar)));
            math.SetMethod("cos", (s, a) => new Instance(Math.Cos((double)a[0].Scalar)));
            math.SetMethod("sin", (s, a) => new Instance(Math.Sin((double)a[0].Scalar)));
            math.SetMethod("abs", (s, a) => new Instance(Math.Abs((double)a[0].Scalar)));
            math.SetMethod("round", (s, a) => new Instance(Math.Round((double)a[0].Scalar)));
            math.SetMethod("max", (s, a) => new Instance(Math.Max(a[0].Scalar, a[1].Scalar)));
            return math;
        }

        private Instance NewStringType()
        {
            var st = Instance.DefineType(new FunctionCode((i, a) =>
            {
                if (i == null)
                    i = new Instance();
                if (a != null && a.Length > 0)
                    i.Scalar = string.Format("{0}", a.First());
                return i;
            }), null, typeof(String));
            st.Prototype.SetField("length", null);/// i => new Instance(i.Scalar.Length, null), null);
            st.Prototype.SetMethod("charAt", (s, a) => new Instance(s.Scalar.Substring(a[0].Scalar, 1)));
            return st;
        }

        private Instance NewObjectType()
        {
            return Instance.DefineType(new FunctionCode((i, a) => { i.Scalar = null; return i; }), null);
        }

        private Instance NewDateType()
        {
            var dt = Instance.DefineType(new FunctionCode((i, a) =>
            {
                i.Scalar = new DateTime();
                return i;
            }), null, typeof(DateTime));
            dt.Prototype.SetMethod("getTime", DateGetTimeFunc);
            return dt;
        }

        private static Instance DateGetTimeFunc(Instance self, params Instance[] args)
        {
            return new Instance((long)(self.Scalar - new DateTime(1970, 1, 1)).TotalMilliseconds);
        }

        private Instance NewArrayType()
        {
            var ar = Instance.DefineType(new FunctionCode(CreateArrayFunc), null);
            ar.SetField("length", null);///i => new Instance(i.MaxIndex + 1, null), null);
            return ar;
        }

        private static Instance CreateArrayFunc(Instance self, Instance[] args)
        {
            if (self == null)
                self = new Instance();
            var arglen = args != null ? args.Length : 0;
            if (arglen == 1)
                ;///self.MaxIndex = (int)(args.First().Scalar) - 1;
            else if (arglen > 1)
                for (var n = 0; n < arglen; n++)
                    self.SetField(n, args[n]);
            return self;
        }

        public Instance GetPrototype(Type type)
        {
            var inst = Fields.FirstOrDefault(f =>
                f.Value.Prototype != null &&
                f.Value.Prototype.NativeTypes != null &&
                f.Value.Prototype.NativeTypes.Contains(type)).Value;
            return inst != null ? inst.Prototype : null;
        }
    }
}
