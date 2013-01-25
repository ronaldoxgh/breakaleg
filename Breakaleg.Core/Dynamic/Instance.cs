using System;
using Breakaleg.Core.Models;

namespace Breakaleg.Core.Dynamic
{
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

        public static Instance operator +(Instance a, Instance b)
        {
            return new Instance(a.Scalar + b.Scalar);
        }

        public static Instance operator -(Instance a, Instance b)
        {
            return new Instance(a.Scalar - b.Scalar);

        }
        public static Instance operator *(Instance a, Instance b)
        {
            return new Instance(a.Scalar * b.Scalar);
        }

        public static Instance operator /(Instance a, Instance b)
        {
            return new Instance(a.Scalar / b.Scalar);
        }
    }
}
