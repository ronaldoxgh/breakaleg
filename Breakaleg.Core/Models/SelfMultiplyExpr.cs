namespace Breakaleg.Core.Models
{
    public class SelfMultiplyExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) * ZeroIfNull(rightValue);
        }
    }
}