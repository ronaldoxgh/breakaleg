using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class WhileCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            NameContext codeContext = null;
            while (Condition.EvalBool(context))
                if (Code != null)
                {
                    if (codeContext == null)
                        codeContext = context.NewChild();
                    var result = Code.Run(codeContext);
                    if (result != null)
                        switch (result.ExitMode)
                        {
                            case ExitMode.Break: break;
                            case ExitMode.Return: return result;
                            case ExitMode.Continue: continue;
                            case ExitMode.Except: return result;
                        }
                }
            return null;
        }

        public override string ToString()
        {
            return string.Format("WHILE({0})", Condition);
        }
    }
}