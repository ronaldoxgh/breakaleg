namespace Breakaleg.Core.Models
{
    public class PreDecExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg)
        {
            updateArg = true;
            return --value;
        }
    }
}