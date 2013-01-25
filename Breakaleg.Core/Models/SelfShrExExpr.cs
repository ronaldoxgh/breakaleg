namespace Breakaleg.Core.Models
{
    public class SelfShrExExpr : SelfAssign
    {
        ///TODO >>>
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return ZeroIfNull(leftValue) >> rightValue;
        }
    }
}