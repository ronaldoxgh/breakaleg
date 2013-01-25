namespace Breakaleg.Core.Models
{
    public class GtExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue > rightValue;
        }
    }
}