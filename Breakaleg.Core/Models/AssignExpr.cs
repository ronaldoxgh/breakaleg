using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class AssignExpr : BinaryExpr, ICallable
    {
        public override Instance Eval(NameContext context)
        {
            var rightInst = RightArg.Eval(context);
            LeftArg.Update(context, rightInst);
            return rightInst;
        }

        public override void Update(NameContext context, Instance inst)
        {
            RightArg.Update(context, inst);
            LeftArg.Update(context, inst);
        }
    }
}