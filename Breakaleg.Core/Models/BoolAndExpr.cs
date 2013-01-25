namespace Breakaleg.Core.Models
{
    public class BoolAndExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return FalseIfNull(leftValue) && FalseIfNull(rightValue);
        }
    }
}