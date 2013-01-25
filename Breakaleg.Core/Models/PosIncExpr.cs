namespace Breakaleg.Core.Models
{
    public class PosIncExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = true;
            return value++;
        }
    }
}