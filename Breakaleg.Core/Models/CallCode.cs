using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class CallCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            Arg.Eval(context);
            return null;
        }

        public override string ToString()
        {
            return string.Format("CALL({0})", Arg);
        }
    }
}