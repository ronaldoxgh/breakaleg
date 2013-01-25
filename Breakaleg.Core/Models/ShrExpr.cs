namespace Breakaleg.Core.Models
{
    public class ShrExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue >> rightValue;
        }
    }
}