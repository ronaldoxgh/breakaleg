namespace Breakaleg.Core.Models
{
    public class SelfShlExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) << ZeroIfNull(rightValue);
        }
    }
}