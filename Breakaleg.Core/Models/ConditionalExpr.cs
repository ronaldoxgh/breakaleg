using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ConditionalExpr : OperationExpr
    {
        public ExprPiece Condition, Then, Else;

        public override Instance Eval(NameContext context)
        {
            var condValue = Condition.EvalScalar(context);
            var retValue = condValue ? Then.EvalScalar(context) : Else.EvalScalar(context);
            return new Instance(retValue);
        }

        public override string ToString()
        {
            return string.Format("{0}?{1}:{2}", Condition, Then, Else);
        }
    }
}