using System.Collections.Generic;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ObjectExpr : ExprPiece
    {
        public Dictionary<string, ExprPiece> Pairs;

        public override Instance Eval(NameContext context)
        {
            var inst = new Instance();
            if (Pairs != null)
                foreach (var pair in Pairs)
                    inst.SetField(pair.Key, pair.Value.Eval(context));
            return inst;
        }

        public override string ToString()
        {
            return "{OBJ}";
        }
    }
}