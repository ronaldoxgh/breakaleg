using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public abstract class SelfAssign : BinaryExpr, ICallable
    {
        public override Instance Eval(NameContext context)
        {
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
            var retValue = ComputeBinary(leftValue, rightValue);
            var retInst = new Instance(retValue);
            LeftArg.Update(context, retInst);
            return retInst;
        }

        protected abstract dynamic ComputeBinary(dynamic leftValue, dynamic rightValue);
    }
}