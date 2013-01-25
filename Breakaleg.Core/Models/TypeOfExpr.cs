using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class TypeOfExpr : OperationExpr
    {
        public ExprPiece Expr;

        public override Instance Eval(NameContext context)
        {
            var obj = (object)Expr.EvalScalar(context);
            return new Instance(obj.GetType());
        }

        public override string ToString()
        {
            return string.Format("TYPEOF({0})", Expr);
        }
    }
}