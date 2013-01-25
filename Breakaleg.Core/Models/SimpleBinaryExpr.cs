using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public abstract class SimpleBinaryExpr : BinaryExpr
    {
        public override Instance Eval(NameContext context)
        {
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
            var retValue = ComputeBinary(leftValue, rightValue);
            return new Instance(retValue);
        }
        protected abstract dynamic ComputeBinary(dynamic leftValue, dynamic rightValue);
    }
}