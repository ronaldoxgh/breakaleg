namespace Breakaleg.Core.Models
{
    public class LtExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue < rightValue;
        }
    }
}