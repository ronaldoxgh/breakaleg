using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace Breakaleg.Core.Models
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

        public virtual void SetMember(object name, Instance inst)
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

        public void Assign(Instance from)
        {
            if (from.members != null && from.members.Count > 0)
            {
                if (this.members == null)
                    this.members = new Dictionary<object, Instance>();
                foreach (var m in from.members)
                    this.members.Add(m.Key, m.Value);
                ///TODO os valores copiados, se sofrerem alteracao, refletem no prototype de volta?
            }
        }

        public Instance[] GetInstanceList()
        {
            if (members == null)
                return null;
            var mar = members.ToArray();
            var insts = new Instance[mar.Length];
            for (var i = 0; i < mar.Length; i++)
                insts[i] = mar[i].Value;
            return insts;
        }

        public class Coisa
        {
            public int ccc;
        }

        protected bool SetObjectMember(object obj, string name, Instance inst)
        {
            Type objType;
            if (obj is ExternalClass)
                objType = (obj as ExternalClass).TypeRef;
            else
                objType = obj.GetType();
            var memberInfo = objType.GetMember(name).FirstOrDefault();
            if (memberInfo == null)
                return false;
            if (memberInfo is PropertyInfo)
                (memberInfo as PropertyInfo).SetValue(obj, inst.ScalarValue, null);
            else if (memberInfo is FieldInfo)
                (memberInfo as FieldInfo).SetValue(obj, inst.ScalarValue);
            else if (memberInfo is MethodInfo)
                throw new Exception("readonly");
            else if (memberInfo is Type)
                throw new Exception("readonly");
            else
                return false;
            return true;
        }

        protected Instance GetObjectMember(object obj, string name)
        {
            if (name == "innerHTML")
                name += "";///x

            Type objType;
            if (obj is ExternalClass)
                objType = (obj as ExternalClass).TypeRef;
            else
                objType = obj.GetType();
            var memberInfo = objType.GetMember(name).FirstOrDefault();
            if (memberInfo == null)
                return null;
            dynamic memberValue = null;
            if (memberInfo is PropertyInfo)
                memberValue = (memberInfo as PropertyInfo).GetValue(obj, null);
            else if (memberInfo is FieldInfo)
                memberValue = (memberInfo as FieldInfo).GetValue(obj);
            else if (memberInfo is MethodInfo)
                memberValue = new ExternalFunction(obj, memberInfo as MethodInfo);
            else if (memberInfo is Type)
                memberValue = new ExternalClass(memberInfo as Type);
            else
                return null;
            return new Instance { ScalarValue = memberValue };
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
                foreach (var ns in namespaces)
                {
                    var inst = GetObjectMember(ns, (string)name);
                    if (inst != null)
                        return inst;
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

        public override Instance GetMember(object name)
        {
            if (name == "innerHTML")
                name += "";///x

            var member = base.GetMember(name);
            if (member == null && name is string && ScalarValue != null)
                member = GetObjectMember(ScalarValue, (string)name);
            return member;
        }

        public override void SetMember(object name, Instance inst)
        {
            if (name is string && ScalarValue != null)
                if (SetObjectMember(ScalarValue, (string)name, inst))
                    return;
            base.SetMember(name, inst);
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

        public override ExitState Run(Context context)
        {
            Context codeContext = null;
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
            } while (Condition.EvalBool(context));
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
                case TKind.Break:
                    return new ExitState { ExitMode = ExitMode.Break };
                case TKind.Continue:
                    return new ExitState { ExitMode = ExitMode.Continue };
                case TKind.Return:
                    return new ExitState { ExitMode = ExitMode.Return, ExitValue = Arg != null ? Arg.Eval(context) : null };
                case TKind.Throw:
                    return new ExitState { ExitMode = ExitMode.Except, ExitValue = Arg != null ? Arg.Eval(context) : null };
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

        public override ExitState Run(Context context)
        {
            var loopContext = context.NewChild();
            Context codeContext = null;
            var setInst = SetExpr.Eval(context);
            var vals = setInst.GetInstanceList();
            if (vals != null)
                foreach (var v in vals)
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
        public string[] Params;
        public CodeBlock Code;

        public override ExitState Run(Context context)
        {
            var selfInst = new Instance { ScalarValue = this, Context = context };
            selfInst.SetMember("prototype", new Instance());
            selfInst.SetMember("length", new Instance { ScalarValue = Params != null ? Params.Length : 0 });
            selfInst.SetMember("constructor", new Instance { ScalarValue = Code });///TODO eh isso?
            context.SetMember(this.Name, selfInst);
            return null;
        }

        public virtual ExitState Call(Context context, Instance owner, Instance[] args)
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

    public class ExternalClass : FunctionCode
    {
        public Type TypeRef;
        public ExternalClass(Type typeRef)
        {
            this.TypeRef = typeRef;
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
            var ret = Activator.CreateInstance(TypeRef, argvals, null);
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
        protected bool callable = false;
        public bool IsCallable { get { return callable; } }

        public dynamic EvalScalar(Context context)
        {
            var inst = Eval(context);
            if (inst != null)
                return inst.ScalarValue;
            return null;
        }

        public bool EvalBool(Context context)
        {
            var inst = Eval(context);
            if (inst == null)
                return false;
            var v = inst.ScalarValue;
            if (v == null)
                return false;
            if (v is string)
                return v.Length > 0;
            Type t = v.GetType();
            if (t.IsAssignableFrom(typeof(byte)))
                return v != 0;
            return v;
        }

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
            if (MemberName == "innerHTML")
                MemberName += "";///x
            var leftInst = LeftArg.Eval(context);
            return leftInst.GetMember(MemberName);
        }

        public override void Update(Context context, Instance inst)
        {
            if (MemberName == "innerHTML")
                MemberName += "";///x
            var leftInst = LeftArg.Eval(context);
            if (leftInst != null)///x
                leftInst.SetMember(MemberName, inst);
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
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
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
            var indexValue = Index.EvalScalar(context);
            return Array.Eval(context).GetMember(indexValue);
        }

        public override void Update(Context context, Instance inst)
        {
            var indexValue = Index.EvalScalar(context);
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
            var protoInst = funcEval.GetMember("prototype");
            if (protoInst != null)
                retInst.Assign(protoInst);
            funcRef.Call(funcEval.Context, retInst, args);
            return retInst;
        }
    }

    public abstract class UnaryExpr : OperationExpr
    {
        public ExprPiece Arg;

        public override Instance Eval(Context context)
        {
            var argValue = Arg.EvalScalar(context);
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
            var obj = (object)Expr.EvalScalar(context);
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
            var leftValue = LeftArg.EvalScalar(context);
            var rightValue = RightArg.EvalScalar(context);
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
            var condValue = Condition.EvalScalar(context);
            dynamic retValue;
            if (condValue)
                retValue = Then.EvalScalar(context);
            else
                retValue = Else.EvalScalar(context);
            return new Instance { ScalarValue = retValue };
        }
    }
}
