namespace Breakaleg.Core.Models
{
    public class GtEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue >= rightValue;
        }
    }
}