namespace Breakaleg.Core.Models
{
    public class MultiplyExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) * ZeroIfNull(rightValue);
        }
    }
}