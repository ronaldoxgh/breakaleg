using System;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class DeleteCode : CodePiece
    {
        public ExprPiece ObjectRef;

        public override ExitResult Run(NameContext context)
        {
            if (ObjectRef is DotExpr)
            {
                var dot = ObjectRef as DotExpr;
                dot.LeftArg.Eval(context).DeleteField(dot.MemberName);
            }
            else if (ObjectRef is NamedExpr)
            {
                var named = ObjectRef as NamedExpr;
                while (context != null)
                {
                    if (context.DeleteField(named.Name))
                        return null;
                    context = context.ParentContext;
                }
            }
            else if (ObjectRef is IndexExpr)
            {
                var idx = ObjectRef as IndexExpr;
                var i = idx.Index.EvalScalar(context);
                idx.Array.Eval(context).DeleteField(i);
            }
            else
                throw new Exception("invalid");
            return null;
        }

        public override string ToString()
        {
            return string.Format("DEL({0})", ObjectRef);
        }
    }
}