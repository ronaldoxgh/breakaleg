namespace Breakaleg.Core.Models
{
    public class PlusExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = false;
            return value;
        }
    }
}