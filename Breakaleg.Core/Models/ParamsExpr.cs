using System;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ParamsExpr : ExprPiece, ICallable
    {
        public ExprPiece FuncExpr;
        public ExprPiece[] Params;

        public Instance[] ToInstances(NameContext context)
        {
            if (Params == null)
                return null;
            var instances = new Instance[Params.Length];
            for (var i = 0; i < Params.Length; i++)
                instances[i] = Params[i].Eval(context);
            return instances;
        }

        public override Instance Eval(NameContext context)
        {
            // se for uma expressao no formato obj.proc(), o obj serah o "this" no contexto
            Instance ownerInst, funcInst;
            if (FuncExpr is DotExpr)
            {
                var dotExpr = FuncExpr as DotExpr;
                dotExpr.GetMethod(context, out ownerInst, out funcInst);
            }
            else
            {
                ownerInst = null;
                funcInst = FuncExpr.Eval(context);
            }
            var args = ToInstances(context);
            if (funcInst.Prototype != null)
            {
                var ret = funcInst.Prototype.Run(ownerInst, args);
                return ret != null ? ret.ExitValue : null;
            }
            throw new Exception("no method");
        }

        public override string ToString()
        {
            return string.Format("{0}(...)", FuncExpr);
        }
    }
}