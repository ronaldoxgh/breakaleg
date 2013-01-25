using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ParensExpr : ExprPiece, ICallable
    {
        public ExprPiece InnerExpr;

        public override Instance Eval(NameContext context)
        {
            return InnerExpr.Eval(context);
        }

        public override string ToString()
        {
            return string.Format("({0})", InnerExpr);
        }
    }
}