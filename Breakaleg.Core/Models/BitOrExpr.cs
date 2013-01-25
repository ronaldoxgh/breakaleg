namespace Breakaleg.Core.Models
{
    public class BitOrExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue | rightValue;
        }
    }
}