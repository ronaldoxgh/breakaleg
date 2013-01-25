namespace Breakaleg.Core.Models
{
    public abstract class BinaryExpr : OperationExpr
    {
        public ExprPiece LeftArg, RightArg;

        public override string ToString()
        {
            return (this.GetType().Name.Replace("Expr", "")) + string.Format("({0};{1})", LeftArg, RightArg);
        }
    }
}