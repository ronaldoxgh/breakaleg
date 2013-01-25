namespace Breakaleg.Core.Models
{
    public class ExactEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue == rightValue && leftValue.GetType() == rightValue.GetType();
        }
    }
}