namespace Breakaleg.Core.Models
{
    public class NegExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = false; 
            return -ZeroIfNull(value);
        }
    }
}