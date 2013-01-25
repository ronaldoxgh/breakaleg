using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class IfCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Then;
        public CodeBlock Else;

        public override ExitResult Run(NameContext context)
        {
            if (Condition.EvalBool(context))
                return Then != null ? Then.Run(context.NewChild()) : null;
            else
                return Else != null ? Else.Run(context.NewChild()) : null;
        }

        public override string ToString()
        {
            return string.Format("IF({0};{1};{2})", Condition, Then, Else);
        }
    }
}