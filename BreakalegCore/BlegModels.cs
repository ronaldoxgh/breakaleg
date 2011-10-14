///TODO Function.prototype

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Breakaleg.Models
{
    public abstract class DynamicRecord
    {
        protected Dictionary<object, Instance> members;

        public virtual Instance GetMember(object name)
        {
            if (members == null)
                return null;
            Instance member;
            members.TryGetValue(name, out member);
            return member;
        }

        public void SetMember(object name, Instance inst)
        {
            if (members == null)
                members = new Dictionary<object, Instance>();
            members[name] = inst;
        }

        public bool DeleteMember(object name)
        {
            if (members != null)
                return members.Remove(name);
            else
                return false;
        }

        public void AddValue(string name, dynamic value)
        {
            SetMember(name, new Instance { ScalarValue = value });
        }
    }

    public class Context : DynamicRecord
    {
        public Context ParentContext;

        public Context NewChild()
        {
            return new Context { ParentContext = this };
        }

        protected List<object> namespaces;
        public void AddNamespace(object ns)
        {
            if (namespaces == null)
                namespaces = new List<object>();
            namespaces.Add(ns);
        }

        public override Instance GetMember(object name)
        {
            return base.GetMember(name) ?? FindNamespace(name);
        }

        private Instance FindNamespace(object name)
        {
            if (name is string && namespaces != null)
            {
                var sname = (string)name;
                foreach (var ns in namespaces)
                {
                    var nst = ns.GetType();
                    var p = nst.GetProperty(sname);
                    if (p != null)
                        return new Instance { ScalarValue = p.GetValue(ns, null) };
                    var f = nst.GetField(sname);
                    if (f != null)
                        return new Instance { ScalarValue = f.GetValue(ns) };
                    var m = nst.GetMethod(sname);
                    if (m != null)
                        return new Instance { ScalarValue = new ExternalFunction(ns, m) };
                }
            }
            return null;
        }
    }

    public class Instance : DynamicRecord
    {
        public dynamic ScalarValue;
        public Context Context;

        public override string ToString()
        {
            return ScalarValue ?? "(null)";
        }
    }

    public enum ExitMode { Normal, Break, Return, Continue, Except }
    public class ExitState
    {
        public ExitMode ExitMode;
        public Instance ExitValue;
    }

    public abstract class CodePiece
    {
        public abstract ExitState Run(Context context);
    }

    public class CodeBlock : CodePiece
    {
        public List<CodePiece> Codes;

        public override ExitState Run(Context context)
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

        public override ExitState Run(Context context)
        {
            if (Condition.Eval(context).ScalarValue)
                return Then != null ? Then.Run(context.NewChild()) : null;
            else
                return Else != null ? Else.Run(context.NewChild()) : null;
        }
    }

    public class WhileCode : CodePiece
    {
        public ExprPiece Condition;
        public CodeBlock Code;

        public override ExitState Run(Context context)
        {
            Context codeContext = null;
            while (Condition.Eval(context).ScalarValue)
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

        public override ExitState Run(Context context)
        {
            Context codeContext = null;
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
            } while (Condition.Eval(context).ScalarValue);
            return null;
        }
    }

    public class BreakCode : CodePiece
    {
        public enum TKind { Break, Continue, Return, Throw }
        public TKind Kind;
        public ExprPiece Arg;

        public override ExitState Run(Context context)
        {
            switch (Kind)
            {
                case TKind.Break: return new ExitState { ExitMode = ExitMode.Break };
                case TKind.Continue: return new ExitState { ExitMode = ExitMode.Continue };
                case TKind.Return: return new ExitState { ExitMode = ExitMode.Return, ExitValue = this.Arg.Eval(context) };
                case TKind.Throw: return new ExitState { ExitMode = ExitMode.Except, ExitValue = this.Arg.Eval(context) };
            }
            return null;
        }
    }

    public class ForCode : CodePiece
    {
        public CodePiece Initialization;
        public ExprPiece Condition;
        public CodePiece Increment;
        public CodeBlock Code;

        public override ExitState Run(Context context)
        {
            var loopContext = context.NewChild();
            Context codeContext = null;
            Initialization.Run(loopContext);
            while (Condition.Eval(loopContext).ScalarValue)
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
                Increment.Run(loopContext);
            }
            return null;
        }
    }

    public class CallCode : CodePiece
    {
        public ExprPiece Arg;

        public override ExitState Run(Context context)
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

        public override ExitState Run(Context context)
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

        public override ExitState Run(Context context)
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

        public bool Match(Context context, Instance switchValue)
        {
            var caseValue = Test.Eval(context);
            return caseValue.ScalarValue == switchValue.ScalarValue;
        }

        public override ExitState Run(Context context)
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

        public override ExitState Run(Context context)
        {
            context.SetMember(this.Name, Value != null ? Value.Eval(context) : null);
            return null;
        }
    }

    public class FunctionCode : CodePiece
    {
        public string Name;
        public List<string> Params;
        public CodeBlock Code;

        public override ExitState Run(Context context)
        {
            context.SetMember(this.Name, new Instance { ScalarValue = this, Context = context });
            return null;
        }

        public virtual ExitState Call(Context context, Instance owner, Instance[] args)
        {
            // nesse callContext serao guardados os parametros e o simbolo this=owner
            var callContext = context.NewChild();
            if (Params != null)
                for (var i = 0; i < Params.Count; i++)
                    callContext.SetMember(Params[i], args != null && i < args.Length ? args[i] : null);
            if (owner != null)
                callContext.SetMember("this", owner);
            return Code.Run(callContext);
        }
    }

    public class ExternalFunction : FunctionCode
    {
        private object target;
        private MethodInfo method;
        public ExternalFunction(object target, MethodInfo method)
        {
            this.target = target;
            this.method = method;
        }

        public override ExitState Run(Context context) { return null; }
        public override ExitState Call(Context context, Instance owner, Instance[] args)
        {
            object[] argvals = null;
            if (args != null)
            {
                argvals = new object[args.Length];
                for (var i = 0; i < args.Length; i++)
                    argvals[i] = args[i].ScalarValue;
            }
            var ret = method.Invoke(target, argvals);
            return new ExitState { ExitMode = ExitMode.Normal, ExitValue = new Instance { ScalarValue = ret } };
        }
    }

    public class DeleteCode : CodePiece
    {
        public ExprPiece ObjectRef;

        public override ExitState Run(Context context)
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
                var i = idx.Index.Eval(context).ScalarValue;
                idx.Array.Eval(context).DeleteMember(i);
            }
            else
                throw new Exception("invalid");
            return null;
        }
    }

    public abstract class ExprPiece
    {
        protected bool callable = false;
        public bool IsCallable { get { return callable; } }

        public abstract Instance Eval(Context context);
        public virtual void Update(Context context, Instance inst) { throw new Exception("leftside is readonly"); }
    }

    public class LiteralConst : ExprPiece
    {
        public dynamic Literal;
        public override Instance Eval(Context context)
        {
            return new Instance { ScalarValue = this.Literal };
        }

        public override string ToString()
        {
            return Literal ?? "(null)";
        }
    }

    public class NamedExpr : ExprPiece
    {
        public string Name;

        public override Instance Eval(Context context)
        {
            while (context != null)
            {
                var member = context.GetMember(this.Name);
                if (member != null)
                    return member;
                context = context.ParentContext;
            }
            return null;
        }

        public override void Update(Context context, Instance inst)
        {
            // o ultimo contexto recebe todas as variaveis sem dono (equiv.: dhtml.window)
            Context defaultContext = null;
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

        public override Instance Eval(Context context)
        {
            return new Instance { ScalarValue = Function, Context = context };
        }

        public override string ToString()
        {
            return Function != null ? Function.ToString() : "(func)";
        }
    }

    public class ArrayExpr : ExprPiece
    {
        public ExprPiece[] Items;

        public override Instance Eval(Context context)
        {
            var inst = new Instance();
            var len = Items != null ? Items.Length : 0;
            for (int i = 0; i < len; i++)
                inst.SetMember(i, Items[i].Eval(context));
            inst.SetMember("length", new Instance { ScalarValue = len });
            return inst;
        }

        public override string ToString()
        {
            return "{...}";
        }
    }

    public class ObjectExpr : ExprPiece
    {
        public Dictionary<string, ExprPiece> Pairs;

        public override Instance Eval(Context context)
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

        public override Instance Eval(Context context)
        {
            return LeftArg.Eval(context).GetMember(MemberName);
        }

        public override void Update(Context context, Instance inst)
        {
            LeftArg.Eval(context).SetMember(MemberName, inst);
        }

        public void GetMethod(Context context, out Instance ownerInst, out Instance funcInst)
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
        public override Instance Eval(Context context)
        {
            var leftValue = LeftArg.Eval(context).ScalarValue;
            var rightValue = RightArg.Eval(context).ScalarValue;
            var retValue = ComputeBinary(leftValue, rightValue);
            return new Instance { ScalarValue = retValue };
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
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue / rightValue; }
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

    public class ShiftLeftExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue << rightValue; }
    }

    public class ShiftRightExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class ShiftRightExExpr : SimpleBinaryExpr
    {
        ///TODO >>>
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class LessExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue < rightValue; }
    }

    public class LessEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue <= rightValue; }
    }

    public class GreaterExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue > rightValue; }
    }

    public class GreaterEqExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >= rightValue; }
    }

    public class EqualExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue == rightValue; }
    }

    public class NotEqualExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue != rightValue; }
    }

    public class ExactEqualExpr : SimpleBinaryExpr
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue == rightValue && leftValue.GetType() == rightValue.GetType(); }
    }

    public class NotExactEqualExpr : SimpleBinaryExpr
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

    public class ParensExpr : ExprPiece
    {
        public ParensExpr() : base() { callable = true; }
        public ExprPiece InnerExpr;
        public override Instance Eval(Context context) { return InnerExpr.Eval(context); }
    }

    public class IndexExpr : ExprPiece
    {
        public ExprPiece Array;
        public ExprPiece Index;

        public override Instance Eval(Context context)
        {
            var indexValue = Index.Eval(context).ScalarValue;
            return Array.Eval(context).GetMember(indexValue);
        }

        public override void Update(Context context, Instance inst)
        {
            var indexValue = Index.Eval(context).ScalarValue;
            Array.Eval(context).SetMember(indexValue, inst);
        }
    }

    public class NewExpr : ExprPiece
    {
        public NewExpr() : base() { callable = true; }

        public ExprPiece Creator;

        public override Instance Eval(Context context)
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
            var funcRef = (FunctionCode)funcEval.ScalarValue;
            var retInst = new Instance();
            funcRef.Call(funcEval.Context, retInst, args);
            return retInst;
        }
    }

    public abstract class UnaryExpr : OperationExpr
    {
        public ExprPiece Arg;

        public override Instance Eval(Context context)
        {
            var argValue = Arg.Eval(context).ScalarValue;
            bool updateArg;
            var retValue = ComputeUnary(ref argValue, out updateArg);
            if (updateArg)
                Arg.Update(context, new Instance { ScalarValue = argValue });
            return new Instance { ScalarValue = retValue };
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

    public class PositiveExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return value; }
    }

    public class NegativeExpr : UnaryExpr
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = false; return -value; }
    }

    public abstract class SelfIncrement : UnaryExpr
    {
        public SelfIncrement() : base() { callable = true; }
    }

    public class PreIncExpr : SelfIncrement
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return ++value; }
    }

    public class PreDecExpr : SelfIncrement
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return --value; }
    }

    public class PosIncExpr : SelfIncrement
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return value++; }
    }

    public class PosDecExpr : SelfIncrement
    {
        protected override dynamic ComputeUnary(ref dynamic value, out bool updateArg) { updateArg = true; return value--; }
    }

    public class ParamsExpr : ExprPiece
    {
        public ParamsExpr() : base() { callable = true; }

        public ExprPiece FuncExpr;
        public ExprPiece[] Params;

        public override Instance Eval(Context context)
        {
            Instance[] args = null;
            if (Params != null)
            {
                args = new Instance[Params.Length];
                for (var i = 0; i < Params.Length; i++)
                    args[i] = Params[i].Eval(context);
            }

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
            var funcRef = (FunctionCode)funcInst.ScalarValue;
            var retInst = funcRef.Call(funcInst.Context, ownerInst, args);
            return retInst != null ? retInst.ExitValue : null;
        }
    }

    public class TypeOfExpr : OperationExpr
    {
        public ExprPiece Expr;

        public override Instance Eval(Context context)
        {
            var obj = (object)Expr.Eval(context).ScalarValue;
            return new Instance { ScalarValue = obj.GetType() };
        }
    }

    public class InstanceOfExpr : OperationExpr
    {
        public override Instance Eval(Context context)
        {
            return null;/// base.Eval(context);///TODO
        }
    }

    public class AssignExpr : BinaryExpr
    {
        public AssignExpr() : base() { callable = true; }

        public override Instance Eval(Context context)
        {
            var rightInst = RightArg.Eval(context);
            LeftArg.Update(context, rightInst);
            return rightInst;
        }

        public override void Update(Context context, Instance inst)
        {
            RightArg.Update(context, inst);
            LeftArg.Update(context, inst);
        }
    }

    public abstract class SelfAssign : BinaryExpr
    {
        public SelfAssign() : base() { callable = true; }

        public override Instance Eval(Context context)
        {
            var leftValue = LeftArg.Eval(context).ScalarValue;
            var rightValue = RightArg.Eval(context).ScalarValue;
            var retValue = ComputeBinary(leftValue, rightValue);
            var retInst = new Instance { ScalarValue = retValue };
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

    public class SelfShiftLeftExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue << rightValue; }
    }

    public class SelfShiftRightExpr : SelfAssign
    {
        protected override dynamic ComputeBinary(dynamic leftValue, dynamic rightValue) { return leftValue >> rightValue; }
    }

    public class SelfShiftRightExExpr : SelfAssign
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
        public override Instance Eval(Context context)
        {
            var condValue = Condition.Eval(context).ScalarValue;
            dynamic retValue;
            if (condValue)
                retValue = Then.Eval(context).ScalarValue;
            else
                retValue = Else.Eval(context).ScalarValue;
            return new Instance { ScalarValue = retValue };
        }
    }
}
