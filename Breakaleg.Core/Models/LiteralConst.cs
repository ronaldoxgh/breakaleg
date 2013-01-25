using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class LiteralConst : ExprPiece
    {
        public dynamic Literal;

        public override Instance Eval(NameContext context)
        {
            return new Instance(this.Literal);
        }

        public override string ToString()
        {
            return Literal != null ? string.Format("{0}", Literal) : "null";
        }
    }
}