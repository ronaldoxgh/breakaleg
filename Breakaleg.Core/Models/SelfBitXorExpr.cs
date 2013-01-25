namespace Breakaleg.Core.Models
{
    public class SelfBitXorExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue ^ rightValue;
        }
    }
}