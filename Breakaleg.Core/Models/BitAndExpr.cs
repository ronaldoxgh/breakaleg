namespace Breakaleg.Core.Models
{
    public class BitAndExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue & rightValue;
        }
    }
}