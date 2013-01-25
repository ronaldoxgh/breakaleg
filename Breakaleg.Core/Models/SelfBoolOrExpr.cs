namespace Breakaleg.Core.Models
{
    public class SelfBoolOrExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return FalseIfNull(leftValue) || FalseIfNull(rightValue);
        }
    }
}