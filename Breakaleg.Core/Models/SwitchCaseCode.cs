using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class SwitchCaseCode : CodePiece
    {
        public ExprPiece Test;
        public CodeBlock Code;

        public bool Match(NameContext context, Instance switchValue)
        {
            var caseValue = Test.Eval(context);
            return caseValue.Scalar == switchValue.Scalar;
        }

        public override ExitResult Run(NameContext context)
        {
            return Code != null ? Code.Run(context) : null;
        }

        public override string ToString()
        {
            return string.Format("CASE({0})", Test);
        }
    }
}