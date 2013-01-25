namespace Breakaleg.Core.Models
{
    public class EqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue == rightValue;
        }
    }
}