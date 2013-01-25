namespace Breakaleg.Core.Models
{
    public class SubtractExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) - ZeroIfNull(rightValue);
        }
    }
}