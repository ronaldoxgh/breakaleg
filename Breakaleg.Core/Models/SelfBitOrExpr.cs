namespace Breakaleg.Core.Models
{
    public class SelfBitOrExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue | rightValue;
        }
    }
}