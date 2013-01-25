using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class DotExpr : ExprPiece
    {
        public ExprPiece LeftArg;
        public string MemberName;

        public override Instance Eval(NameContext context)
        {
            var leftInst = LeftArg.Eval(context);
            var member = leftInst.GetField(MemberName);
            return member;
        }

        public override void Update(NameContext context, Instance inst)
        {
            var leftInst = LeftArg.Eval(context);
            leftInst.SetField(MemberName, inst);
        }

        public void GetMethod(NameContext context, out Instance ownerInst, out Instance funcInst)
        {
            ownerInst = LeftArg.Eval(context);
            funcInst = ownerInst.GetField(MemberName);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", LeftArg, MemberName);
        }
    }
}