namespace Breakaleg.Core.Models
{
    public class ModulusExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            return leftValue % rightValue;
        }
    }
}