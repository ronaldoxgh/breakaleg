namespace Breakaleg.Core.Models
{
    public class NotEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue != rightValue;
        }
    }
}