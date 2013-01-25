namespace Breakaleg.Core.Models
{
    public class DivideExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            if ((rightValue = ZeroIfNull(rightValue)) == 0)
                return double.NaN;
            return ZeroIfNull(leftValue) / rightValue;
        }
    }
}