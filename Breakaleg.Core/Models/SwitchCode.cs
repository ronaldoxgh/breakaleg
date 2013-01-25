using System.Collections.Generic;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class SwitchCode : CodePiece
    {
        public ExprPiece Arg;
        public List<SwitchCaseCode> Cases;
        public CodeBlock Default;

        public override ExitResult Run(NameContext context)
        {
            var switchContext = context.NewChild();
            var argInst = Arg.Eval(switchContext);
            var found = false;
            foreach (var someCase in Cases)
                if (someCase.Match(switchContext, argInst))
                {
                    found = true;
                    var caseResult = someCase.Run(switchContext);
                    if (caseResult != null)
                        switch (caseResult.ExitMode)
                        {
                            case ExitMode.Break: return null;
                            case ExitMode.Return: return caseResult;
                            case ExitMode.Continue: return caseResult;
                            case ExitMode.Except: return caseResult;
                        }
                }
            if (!found && Default != null)
                return Default.Run(switchContext);
            return null;
        }

        public override string ToString()
        {
            return string.Format("SWITCH({0})", Arg);
        }
    }
}