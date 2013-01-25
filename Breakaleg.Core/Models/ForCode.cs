using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ForCode : CodePiece
    {
        public CodePiece Initialization;
        public ExprPiece Condition;
        public CodePiece Increment;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            var loopContext = context.NewChild();
            NameContext codeContext = null;
            if (Initialization != null)
                Initialization.Run(loopContext);
            while (Condition == null || Condition.EvalBool(loopContext))
            {
                if (Code != null)
                {
                    if (codeContext == null)
                        codeContext = loopContext.NewChild();
                    var result = Code.Run(codeContext);
                    if (result != null)
                        switch (result.ExitMode)
                        {
                            case ExitMode.Break: break;
                            case ExitMode.Return: return result;
                            case ExitMode.Continue: goto INC;
                            case ExitMode.Except: return result;
                        }
                }
                INC:
                if (Increment != null)
                    Increment.Run(loopContext);
            }
            return null;
        }

        public override string ToString()
        {
            return string.Format("FOR({0};{1};{2})", Initialization, Condition, Increment);
        }
    }
}