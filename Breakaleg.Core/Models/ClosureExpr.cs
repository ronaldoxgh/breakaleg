using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ClosureExpr : ExprPiece
    {
        public FunctionCode Function;

        public override Instance Eval(NameContext context)
        {
            return Instance.DefineType(this.Function, context);
        }

        public override string ToString()
        {
            return string.Format("CLOSURE({0})", Function);
        }
    }
}