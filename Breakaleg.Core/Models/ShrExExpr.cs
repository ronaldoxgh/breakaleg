namespace Breakaleg.Core.Models
{
    public class ShrExExpr : SimpleBinaryExpr
    {
        ///TODO >>>
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue >> rightValue;
        }
    }
}