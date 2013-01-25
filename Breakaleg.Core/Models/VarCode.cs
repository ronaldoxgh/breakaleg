using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class VarCode : CodePiece
    {
        public string Name;
        public ExprPiece Value;

        public override ExitResult Run(NameContext context)
        {
            context.SetField(this.Name, Value != null ? Value.Eval(context) : null);
            return null;
        }

        public override string ToString()
        {
            return string.Format("VAR({0}={1})", Name, Value);
        }
    }
}