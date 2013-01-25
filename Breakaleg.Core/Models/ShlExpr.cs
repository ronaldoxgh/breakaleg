namespace Breakaleg.Core.Models
{
    public class ShlExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue << rightValue;
        }
    }
}