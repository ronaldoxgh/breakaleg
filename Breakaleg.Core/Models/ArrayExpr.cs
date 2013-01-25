using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ArrayExpr : ExprPiece
    {
        public ExprPiece[] Items;

        public override Instance Eval(NameContext context)
        {
            var inst = new Instance(null);
            var len = Items != null ? Items.Length : 0;
            for (var i = 0; i < len; i++)
                inst.SetField(i, Items[i].Eval(context));
            inst.SetField("length", new Instance(len));
            return inst;
        }

        public override string ToString()
        {
            return "[...]";
        }
    }
}