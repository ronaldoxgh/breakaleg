using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public abstract class UnaryExpr : OperationExpr
    {
        public ExprPiece Arg;

        public override Instance Eval(NameContext context)
        {
            var argValue = Arg.EvalScalar(context);
            bool updateArg;
            var retValue = ComputeUnary(ref argValue, out updateArg);
            if (updateArg)
                Arg.Update(context, new Instance(argValue));
            return new Instance(retValue);
        }

        protected abstract dynamic ComputeUnary(ref dynamic value, out bool updateArg);

        public override string ToString()
        {
            return this.GetType().Name.Replace("Expr", "") + string.Format("({0})", Arg);
        }
    }
}