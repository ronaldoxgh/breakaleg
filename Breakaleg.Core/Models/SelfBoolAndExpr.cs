namespace Breakaleg.Core.Models
{
    public class SelfBoolAndExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return FalseIfNull(leftValue) && FalseIfNull(rightValue);
        }
    }
}