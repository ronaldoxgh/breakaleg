using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ThrowCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            return new ExitResult
            {
                ExitMode = ExitMode.Except,
                ExitValue = Arg != null ? Arg.Eval(context) : null
            };
        }

        public override string ToString()
        {
            return string.Format("THROW({0})", Arg);
        }
    }
}