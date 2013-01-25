namespace Breakaleg.Core.Models
{
    public class BitXorExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue ^ rightValue;
        }
    }
}