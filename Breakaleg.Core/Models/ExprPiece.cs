using System;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public abstract class ExprPiece
    {
        protected static dynamic ZeroIfNull(dynamic value)
        {
            if (value == null)
                return 0;
            return value;
        }

        protected static dynamic FalseIfNull(dynamic value)
        {
            if (value == null)
                return false;
            return value;
        }

        public dynamic EvalScalar(NameContext context)
        {
            var inst = Eval(context);
            return inst != null ? inst.Scalar : null;
        }

        public bool EvalBool(NameContext context)
        {
            var inst = Eval(context);
            if (inst == null)
                return false;
            var v = inst.Scalar;
            if (v == null)
                return false;
            if (v is string)
                return v.Length > 0;
            Type t = v.GetType();
            if (t.IsAssignableFrom(typeof(byte)))
                return v != 0;
            return v;
        }

        public abstract Instance Eval(NameContext context);

        public virtual void Update(NameContext context, Instance inst) { throw new Exception("leftside is readonly"); }
    }
}