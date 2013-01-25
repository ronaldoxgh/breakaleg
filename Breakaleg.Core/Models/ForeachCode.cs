using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ForeachCode : CodePiece
    {
        public string VarName;
        public ExprPiece SetExpr;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            var loopContext = context.NewChild();
            NameContext codeContext = null;
            var setInst = SetExpr.Eval(context);
            var instList = setInst.GetFields();
            if (instList != null)
                foreach (var inst in instList)
                    if (Code != null)
                    {
                        loopContext.SetField(VarName, inst);
                        if (codeContext == null)
                            codeContext = loopContext.NewChild();
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
            return string.Format("FOREACH({0};{1})", VarName, SetExpr);
        }
    }
}