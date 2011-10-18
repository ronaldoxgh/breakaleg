using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Breakaleg.Core.Models
{
    public enum ExitMode { Normal, Break, Return, Continue, Except }
    public class ExitResult
    {
        public ExitMode ExitMode;
        public Instance ExitValue;
    }

    public abstract class CodePiece
    {
        public abstract ExitResult Run(NameContext context);
    }

    public class CodeBlock : CodePiece
    {
        public List<CodePiece> Codes;

        public override ExitResult Run(NameContext context)
        {
            if (Codes == null || Codes.Count == 0)
                return null;
            foreach (var code in Codes)
            {
                var result = code.Run(context);
                if (result != null && result.ExitMode != ExitMode.Normal)
                    return result;
            }
            return null;
        }
    }

    public class IfCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Then;
        public CodeBlock Else;

        public override ExitResult Run(NameContext context)
        {
            if (Condition.EvalBool(context))
                return Then != null ? Then.Run(context.NewChild()) : null;
            else
                return Else != null ? Else.Run(context.NewChild()) : null;
        }
    }

    public class WhileCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            NameContext codeContext = null;
            while (Condition.EvalBool(context))
                if (Code != null)
                {
                    if (codeContext == null)
                        codeContext = context.NewChild();
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
    }

    public class UntilCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            NameContext codeContext = null;
            do
            {
                if (Code != null)
                {
                    if (codeContext == null)
                        codeContext = context.NewChild();
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
            } while (Condition.EvalBool(context));
            return null;
        }
    }

    public class BreakCode : CodePiece
    {
        public override ExitResult Run(NameContext context)
        {
            return new ExitResult { ExitMode = ExitMode.Break };
        }
    }

    public class ContinueCode : CodePiece
    {
        public override ExitResult Run(NameContext context)
        {
            return new ExitResult { ExitMode = ExitMode.Continue };
        }
    }

    public class ReturnCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            return new ExitResult { ExitMode = ExitMode.Return, ExitValue = Arg != null ? Arg.Eval(context) : null };
        }
    }

    public class ThrowCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            return new ExitResult { ExitMode = ExitMode.Except, ExitValue = Arg != null ? Arg.Eval(context) : null };
        }
    }

    public class ForCode : CodePiece
    {
        public CodePiece Initialization;
        public ExprPiece Condition;
        public CodePiece Increment;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            var loopContext = context.NewChild();
            NameContext codeContext = null;
            if (Initialization != null)
                Initialization.Run(loopContext);
            while (Condition == null || Condition.EvalBool(loopContext))
            {
                if (Code != null)
                {
                    if (codeContext == null)
                        codeContext = loopContext.NewChild();
                    var result = Code.Run(codeContext);
                    if (result != null)
                        switch (result.ExitMode)
                        {
                            case ExitMode.Break: break;
                            case ExitMode.Return: return result;
                            case ExitMode.Continue: goto INC;
                            case ExitMode.Except: return result;
                        }
                }
            INC:
                if (Increment != null)
                    Increment.Run(loopContext);
            }
            return null;
        }
    }

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
            var instList = setInst.GetMembers();
            if (instList != null)
                foreach (var inst in instList)
                    if (Code != null)
                    {
                        loopContext.SetMember(VarName, inst);
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
    }

    public class CallCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitResult Run(NameContext context)
        {
            Arg.Eval(context);
            return null;
        }
    }

    public class TryCode : CodePiece
    {
        public CodeBlock Code;
        public CodeBlock Finally;
        public CodeBlock Catch;
        public string CatchName;

        public override ExitResult Run(NameContext context)
        {
            var result = Code.Run(context.NewChild());
            if (result != null && result.ExitMode == ExitMode.Except)
                if (Catch != null)
                {
                    var catchResult = Catch.Run(context.NewChild());
                    if (catchResult != null && catchResult.ExitMode != ExitMode.Normal)
                        return catchResult;
                    result = null;
                }
            if (Finally != null)
            {
                var finalResult = Finally.Run(context.NewChild());
                if (finalResult != null && finalResult.ExitMode != ExitMode.Normal)
                    return finalResult;
            }
            return result;
        }
    }

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
    }

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
            if (Code != null)
                return Code.Run(context);
            return null;
        }
    }

    public class VarCode : CodePiece
    {
        public string Name;
        public ExprPiece Value;

        public override ExitResult Run(NameContext context)
        {
            context.SetMember(this.Name, Value != null ? Value.Eval(context) : null);
            return null;
        }
    }

    public class FunctionCode : CodePiece
    {
        public string Name;
        public string[] Params;
        public CodeBlock Code;

        public override ExitResult Run(NameContext context)
        {
            var selfInst = new Instance(this, context);
            selfInst.SetMember("prototype", new Instance());
            selfInst.SetMember("length", new Instance(Params != null ? Params.Length : 0, null));
            selfInst.SetMember("constructor", new Instance(Code, null));///TODO eh isso?
            context.SetMember(this.Name, selfInst);
            return null;
        }

        public ExitResult Call(NameContext context, Instance owner, Instance[] args)
        {
            // nesse callContext serao guardados os parametros e o simbolo this=owner
            var callContext = context.NewChild();
            if (Params != null)
                for (var i = 0; i < Params.Length; i++)
                    callContext.SetMember(Params[i], args != null && i < args.Length ? args[i] : null);
            if (owner != null)
                callContext.SetMember("this", owner);
            return Code.Run(callContext);
        }
    }

    public class DeleteCode : CodePiece
    {
        public ExprPiece ObjectRef;

        public override ExitResult Run(NameContext context)
        {
            if (ObjectRef is DotExpr)
            {
                var dot = ObjectRef as DotExpr;
                dot.LeftArg.Eval(context).DeleteMember(dot.MemberName);
            }
            else if (ObjectRef is NamedExpr)
            {
                var named = ObjectRef as NamedExpr;
                while (context != null)
                {
                    if (context.DeleteMember(named.Name))
                        return null;
                    context = context.ParentContext;
                }
            }
            else if (ObjectRef is IndexExpr)
            {
                var idx = ObjectRef as IndexExpr;
                var i = idx.Index.EvalScalar(context);
                idx.Array.Eval(context).DeleteMember(i);
            }
            else
                throw new Exception("invalid");
            return null;
        }
    }

    public abstract class ExprPiece
    {
        public dynamic EvalScalar(NameContext context)
        {
            var inst = Eval(context);
            return inst != null ? inst.Scalar : null;
        }

        public bool EvalBool(NameContext context)
        {
            var inst = Eval(context);
            if (inst == null)
                return false;
            var v = inst.Scalar;
            if (v == null)
                return false;
            if (v is string)
                return v.Length > 0;
            Type t = v.GetType();
            if (t.IsAssignableFrom(typeof(byte)))
                return v != 0;
            return v;
        }

        public abstract Instance Eval(NameContext context);
        public virtual void Update(NameContext context, Instance inst) { throw new Exception("leftside is readonly"); }
    }

    public class LiteralConst : ExprPiece
    {
        public dynamic Literal;
        public override Instance Eval(NameContext context)
        {
            return new Instance(this.Literal, null);
        }

        public override string ToString()
        {
            return Literal ?? "(null)";
        }
    }

    public class NamedExpr : ExprPiece
    {
        public string Name;

        public override Instance Eval(NameContext context)
        {
            while (context != null)
            {
                var member = context.GetMember(this.Name);
                if (member != null)
                    return member;
                context = context.ParentContext;
            }
            throw new Exception(string.Format("undefined '{0}'", Name));
        }

        public override void Update(NameContext context, Instance inst)
        {
            // o ultimo contexto recebe todas as variaveis sem dono (equiv.: dhtml.window)
            NameContext defaultContext = null;
            while (context != null)
            {
                if (context.GetMember(this.Name) != null)
                {
                    context.SetMember(this.Name, inst);
                    return;
                }
                defaultContext = context;
                context = context.ParentContext;
            }
            if (defaultContext != null)
                defaultContext.SetMember(this.Name, inst);
        }

        public override string ToString()
        {
            return Name ?? "(named)";
        }
    }

    public class FunctionExpr : ExprPiece
    {
        public FunctionCode Function;

        public override Instance Eval(NameContext context)
        {
            return new Instance(Function, context);
        }

        public override string ToString()
        {
            return Function != null ? Function.ToString() : "(func)";
        }
    }

    public class ArrayExpr : ExprPiece
    {
        public ExprPiece[] Items;

        public override Instance Eval(NameContext context)
        {
            var inst = new Instance(null, null);
            var len = Items != null ? Items.Length : 0;
            for (int i = 0; i < len; i++)
                inst.SetMember(i, Items[i].Eval(context));
            inst.SetMember("length", new Instance(len, null));
            return inst;
        }

        public override string ToString()
        {
            return "[...]";
        }
    }

    public class ObjectExpr : ExprPiece
    {
        public Dictionary<string, ExprPiece> Pairs;

        public override Instance Eval(NameContext context)
        {
            var inst = new Instance();
            if (Pairs != null)
                foreach (var pair in Pairs)
                    inst.SetMember(pair.Key, pair.Value.Eval(context));
            return inst;
        }

        public override string ToString()
        {
            return "(obj)";
        }
    }

    public class DotExpr : ExprPiece
    {
        public ExprPiece LeftArg;
        public string MemberName;

        public override Instance Eval(NameContext context)
        {
            var leftInst = LeftArg.Eval(context);
            return leftInst.GetMember(MemberName);
        }

        public override void Update(NameContext context, Instance inst)
        {
            var leftInst = LeftArg.Eval(context);
            if (leftInst != null)///TODO necessary?
                leftInst.SetMember(MemberName, inst);
        }

        public void GetMethod(NameContext context, out Instance ownerInst, out Instance funcInst)
        {
            ownerInst = LeftArg.Eval(context);
            funcInst = ownerInst.GetMember(MemberName);
        }

        public override string ToString()
        {
            return LeftArg.ToString() + "." + MemberName;
        }
    }

    public abstract class OperationExpr : ExprPiece { }

    public abstract class BinaryExpr : OperationExpr
    {
        public ExprPiece LeftArg, RightArg;
    }

    public abstract class SimpleBinaryExpr : BinaryExpr
    {
        public override Instance Eval(NameContext context)
        {
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
            var retValue = ComputeBinary(leftValue, rightValue);
            return new Instance(retValue, null);
        }
        protected abstract dynamic ComputeBinary(dynamic leftValue, dynamic rightValue);
    }

    public class SumExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue + rightValue; }
    }

    public class SubtractExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue - rightValue; }
    }

    public class MultiplyExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue * rightValue; }
    }

    public class DivideExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue)
        {
            if (rightValue == 0)
                return double.NaN;
            return leftValue / rightValue;
        }
    }

    public class BoolAndExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue && rightValue; }
    }

    public class BoolOrExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue || rightValue; }
    }

    public class ModulusExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue % rightValue; }
    }

    public class ShlExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue << rightValue; }
    }

    public class ShrExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class ShrExExpr : SimpleBinaryExpr
    {
        ///TODO >>>
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class LtExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue < rightValue; }
    }

    public class LtEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue <= rightValue; }
    }

    public class GtExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue > rightValue; }
    }

    public class GtEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >= rightValue; }
    }

    public class EqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue == rightValue; }
    }

    public class NotEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue != rightValue; }
    }

    public class ExactEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue == rightValue && leftValue.GetType() == rightValue.GetType(); }
    }

    public class NotExactEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue != rightValue || leftValue.GetType() != rightValue.GetType(); }
    }

    public class BitAndExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue & rightValue; }
    }

    public class BitOrExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue | rightValue; }
    }

    public class BitXorExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue ^ rightValue; }
    }

    public class ParensExpr : ExprPiece, ICallable
    {
        public ExprPiece InnerExpr;
        public override Instance Eval(NameContext context) { return InnerExpr.Eval(context); }
    }

    public class IndexExpr : ExprPiece
    {
        public ExprPiece Array;
        public ExprPiece Index;

        public override Instance Eval(NameContext context)
        {
            var indexValue = Index.EvalScalar(context);
            return Array.Eval(context).GetMember(indexValue);
        }

        public override void Update(NameContext context, Instance inst)
        {
            var indexValue = Index.EvalScalar(context);
            Array.Eval(context).SetMember(indexValue, inst);
        }
    }

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
            var retInst = funcEval.CreateNew(funcEval.ParentContext, args);
            return retInst;
        }
    }

    public abstract class UnaryExpr : OperationExpr
    {
        public ExprPiece Arg;

        public override Instance Eval(NameContext context)
        {
            var argValue = Arg.EvalScalar(context);
            bool updateArg;
            var retValue = ComputeUnary(ref argValue, out updateArg);
            if (updateArg)
                Arg.Update(context, new Instance(argValue, null));
            return new Instance(retValue, null);
        }
        protected abstract dynamic ComputeUnary(ref dynamic value, out bool updateArg);
    }

    public class BoolNotExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return !value; }
    }

    public class BitNotExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return ~value; }
    }

    public class PlusExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return value; }
    }

    public class NegExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return -value; }
    }

    public abstract class SelfOpExpr : UnaryExpr, ICallable { }

    public class PreIncExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return ++value; }
    }

    public class PreDecExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return --value; }
    }

    public class PosIncExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return value++; }
    }

    public class PosDecExpr : SelfOpExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return value--; }
    }

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
            var ret = funcInst.Execute(context, ownerInst, args);
            return ret != null ? ret.ExitValue : null;
        }
    }

    public class TypeOfExpr : OperationExpr
    {
        public ExprPiece Expr;

        public override Instance Eval(NameContext context)
        {
            var obj = (object)Expr.EvalScalar(context);
            return new Instance(obj.GetType(), null);
        }
    }

    public class InstanceOfExpr : OperationExpr
    {
        public override Instance Eval(NameContext context)
        {
            return null;/// base.Eval(context);///TODO
        }
    }

    public class AssignExpr : BinaryExpr, ICallable
    {
        public override Instance Eval(NameContext context)
        {
            var rightInst = RightArg.Eval(context);
            LeftArg.Update(context, rightInst);
            return rightInst;
        }

        public override void Update(NameContext context, Instance inst)
        {
            RightArg.Update(context, inst);
            LeftArg.Update(context, inst);
        }
    }

    public abstract class SelfAssign : BinaryExpr, ICallable
    {
        public override Instance Eval(NameContext context)
        {
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
            var retValue = ComputeBinary(leftValue, rightValue);
            var retInst = new Instance(retValue, null);
            LeftArg.Update(context, retInst);
            return retInst;
        }

        protected abstract dynamic ComputeBinary(dynamic leftValue, dynamic rightValue);
    }

    public class SelfSumExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue + rightValue; }
    }

    public class SelfSubtractExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue - rightValue; }
    }

    public class SelfMultiplyExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue * rightValue; }
    }

    public class SelfDivideExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue / rightValue; }
    }

    public class SelfModulusExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue % rightValue; }
    }

    public class SelfShlExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue << rightValue; }
    }

    public class SelfShrExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class SelfShrExExpr : SelfAssign
    {
        ///TODO >>>
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class SelfBitAndExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue & rightValue; }
    }

    public class SelfBitXorExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue ^ rightValue; }
    }

    public class SelfBitOrExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue | rightValue; }
    }

    public class SelfBoolAndExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue && rightValue; }
    }

    public class SelfBoolOrExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue || rightValue; }
    }

    public class ConditionalExpr : OperationExpr
    {
        public ExprPiece Condition, Then, Else;
        public override Instance Eval(NameContext context)
        {
            var condValue = Condition.EvalScalar(context);
            dynamic retValue;
            if (condValue)
                retValue = Then.EvalScalar(context);
            else
                retValue = Else.EvalScalar(context);
            return new Instance(retValue, null);
        }
    }
}
