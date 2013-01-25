using System;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class NamedExpr : ExprPiece
    {
        public string Name;

        public override Instance Eval(NameContext context)
        {
            var inst = context.GetFieldUpwards(this.Name);
            if (inst == null)
                throw new Exception(string.Format("undefined '{0}'", this.Name));
            return inst;
        }

        public override void Update(NameContext context, Instance inst)
        {
            context.SetFieldUpwards(this.Name, inst);
        }

        public override string ToString()
        {
            return Name ?? "(named)";
        }
    }
}