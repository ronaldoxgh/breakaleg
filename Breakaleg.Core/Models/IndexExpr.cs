using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class IndexExpr : ExprPiece
    {
        public ExprPiece Array;
        public ExprPiece Index;

        public override Instance Eval(NameContext context)
        {
            var indexValue = Index.EvalScalar(context);
            var arrayInst = Array.Eval(context);
            var member = arrayInst.GetField(indexValue);
            return member;
        }

        public override void Update(NameContext context, Instance inst)
        {
            var indexValue = Index.EvalScalar(context);
            var arrayInst = Array.Eval(context);
            arrayInst.SetField(indexValue, inst);
        }

        public override string ToString()
        {
            return string.Format("{0}[{1}]", Array, Index);
        }
    }
}