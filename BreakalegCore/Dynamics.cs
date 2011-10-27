using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;

namespace Breakaleg.Core.Models
{
    public delegate Instance NativeCodeCallback(Instance self, params Instance[] args);

    public abstract class DynamicRecord
    {
        protected Dictionary<object, Instance> fields;

        public void ShareFieldsWith(DynamicRecord recTo)
        {
            if (this.fields == null)
                this.fields = new Dictionary<object, Instance>();
            recTo.fields = this.fields;
        }

        public virtual Instance GetField(object name)
        {
            Instance f;
            if (fields != null && fields.TryGetValue(name, out f))
                return f;
            return null;
        }

        public Instance SetField(object name, Instance inst)
        {
            (fields ?? (fields = new Dictionary<object, Instance>()))[name] = inst;
            return inst;
        }

        public bool DeleteField(object name)
        {
            return fields != null ? fields.Remove(name) : false;
        }

        public Instance SetMethod(object name, NativeCodeCallback nativeCode)
        {
            return SetField(name, new Instance { Prototype = new Instance { Code = new FunctionCode(nativeCode) } });
        }

        public Instance[] GetFields()
        {
            if (fields == null)
                return null;
            var fieldArray = fields.ToArray();
            var instArray = new Instance[fieldArray.Length];
            for (var i = 0; i < fieldArray.Length; i++)
                instArray[i] = fieldArray[i].Value;
            return instArray;
        }
    }

    public class Instance : DynamicRecord
    {
        // prototipo do meu tipo se eu for uma variavel
        private Instance baseType;
        public Instance BaseType { get { return GetBaseType(); } set { this.baseType = value; } }

        private Instance GetBaseType()
        {
            if (baseType == null && Scalar != null)
            {
                baseType = JSNames.Running.GetPrototype(Scalar.GetType());
                if (baseType == null)
                    throw new Exception("lib not loaded");
            }
            return baseType;
        }

        public Instance Prototype; // meu proprio prototipo se eu for uma funcao
        public NameContext CodeContext; // contexto da funcao
        public FunctionCode Code; // codigo de inicializacao se eu for um construtor; ou codigo da funcao
        public dynamic Scalar;
        public Type[] NativeTypes;

        public Instance() { }
        public Instance(dynamic scalar)
        {
            this.Scalar = scalar;
        }

        public override Instance GetField(object name)
        {
            var f = base.GetField(name);
            if (f == null && name is string)
                // o unico cara q tem a prop prototype eh uma funcao (ex.: Pessoa.prototype)
                if ((string)name == "prototype")
                    f = this.Prototype;
                else if (this.BaseType != null)
                    f = this.BaseType.GetField(name);
            return f;
        }

        public static Instance DefineType(FunctionCode constructor, NameContext typecontext, params Type[] nativeTypes)
        {
            return new Instance
            {
                Prototype = new Instance
                {
                    Code = constructor,
                    CodeContext = typecontext,
                    NativeTypes = nativeTypes,
                }
            };
        }

        public Instance New(Instance[] args)
        {
            var inst = new Instance { BaseType = this };
            Code.Call(this.CodeContext, inst, args);
            return inst;
        }

        public ExitResult Run(Instance owner, Instance[] args)
        {
            return Code.Call(this.CodeContext, owner, args);
        }

        public override string ToString()
        {
            return Scalar != null ? Scalar.ToString() : "null";
        }

        public static Instance operator +(Instance a, Instance b) { return new Instance(a.Scalar + b.Scalar); }
        public static Instance operator -(Instance a, Instance b) { return new Instance(a.Scalar - b.Scalar); }
        public static Instance operator *(Instance a, Instance b) { return new Instance(a.Scalar * b.Scalar); }
        public static Instance operator /(Instance a, Instance b) { return new Instance(a.Scalar / b.Scalar); }
    }

    public interface ICallable { }

    public class NameContext : DynamicRecord
    {
        private NameContext parentContext;
        public NameContext ParentContext { get { return this.parentContext; } }

        public NameContext NewChild()
        {
            return new NameContext { parentContext = this };
        }

        public Instance GetFieldUpwards(object name)
        {
            var temp = this;
            while (temp != null)
            {
                var field = temp.GetField(name);
                if (field != null)
                    return field;
                temp = temp.ParentContext;
            }
            return null;
        }

        public void SetFieldUpwards(object name, Instance inst)
        {
            // o ultimo contexto recebe todas as variaveis sem dono (equiv.: dhtml.window)
            NameContext defaultContext = null;
            var temp = this;
            while (temp != null)
            {
                if (temp.GetField(name) != null)
                {
                    temp.SetField(name, inst);
                    return;
                }
                defaultContext = temp;
                temp = temp.ParentContext;
            }
            if (defaultContext != null)
                defaultContext.SetField(name, inst);
        }
    }

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
            var dt = Instance.DefineType(new FunctionCode((i, a) => { i.Scalar = new DateTime(); return i; }), null, typeof(DateTime));
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
            var inst = fields.FirstOrDefault(f =>
                f.Value.Prototype != null &&
                f.Value.Prototype.NativeTypes != null &&
                f.Value.Prototype.NativeTypes.Contains(type)).Value;
            return inst != null ? inst.Prototype : null;
        }
    }
}
