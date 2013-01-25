namespace Breakaleg.Core.Models
{
    public class SelfBitAndExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue & rightValue;
        }
    }
}