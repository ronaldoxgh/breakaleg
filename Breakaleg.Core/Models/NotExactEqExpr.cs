namespace Breakaleg.Core.Models
{
    public class NotExactEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue != rightValue || leftValue.GetType() != rightValue.GetType();
        }
    }
}