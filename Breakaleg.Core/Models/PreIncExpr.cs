namespace Breakaleg.Core.Models
{
    public class PreIncExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = true;
            return ++value;
        }
    }
}