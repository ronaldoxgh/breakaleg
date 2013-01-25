using System;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class NewExpr : ExprPiece, ICallable
    {
        public ExprPiece Creator;

        public override Instance Eval(NameContext context)
        {
            Instance[] args = null;
            ExprPiece funcExpr;
            if (Creator is ParamsExpr)
            {
                var paramExpr = Creator as ParamsExpr;
                if (paramExpr.Params != null)
                {
                    args = new Instance[paramExpr.Params.Length];
                    for (var i = 0; i < paramExpr.Params.Length; i++)
                        args[i] = paramExpr.Params[i].Eval(context);
                }
                funcExpr = paramExpr.FuncExpr;
            }
            else
                funcExpr = Creator;
            var funcEval = funcExpr.Eval(context);
            if (funcEval.Prototype != null)
                return funcEval.Prototype.New(args);
            throw new Exception("no constructor");
        }

        public override string ToString()
        {
            return string.Format("NEW({0})", Creator);
        }
    }
}