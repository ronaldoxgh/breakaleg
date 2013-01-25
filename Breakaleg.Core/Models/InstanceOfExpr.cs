using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class InstanceOfExpr : OperationExpr
    {
        public override Instance Eval(NameContext context)
        {
            return null;/// base.Eval(context);///TODO
        }

        public override string ToString()
        {
            return "IS()";
        }
    }
}