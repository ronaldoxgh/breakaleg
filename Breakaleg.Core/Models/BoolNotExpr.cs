namespace Breakaleg.Core.Models
{
    public class BoolNotExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = false;
            return !FalseIfNull(value);
        }
    }
}