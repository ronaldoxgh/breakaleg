namespace Breakaleg.Core.Models
{
    public class LtEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue <= rightValue;
        }
    }
}