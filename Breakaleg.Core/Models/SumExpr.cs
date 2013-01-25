namespace Breakaleg.Core.Models
{
    public class SumExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) + ZeroIfNull(rightValue);
        }
    }
}