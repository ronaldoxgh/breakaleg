using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ReturnCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            return new ExitResult
            {
                ExitMode = ExitMode.Return,
                ExitValue = Arg != null ? Arg.Eval(context) : null
            };
        }

        public override string ToString()
        {
            return string.Format("RET({0})", Arg);
        }
    }
}