///TODO ternary conditional op "?:"
///TODO for...in...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Breakaleg.Core.Readers;
using Breakaleg.Core.Models;

namespace Breakaleg.Core.Compilers
{
    class JSReader : StringReader
    {
        public JSReader(string code) : base(code) { }
        public override bool Comments()
        {
            return LineComment() || BlockComment();
        }

        private bool BlockComment()
        {
            if (ThisTextNoSkip("/*"))
            {
                char ch;
                while (AnyChar(out ch))
                    if (ch == '*')
                        if (ThisCharNoSkip('/'))
                            return true;
            }
            return false;
        }

        private bool LineComment()
        {
            if (ThisTextNoSkip("//"))
            {
                char ch;
                while (AnyChar(out ch))
                    if (ch == '\r')
                    {
                        ThisCharNoSkip('\n');
                        return true;
                    }
                    else if (ch == '\n')
                        return true;
            }
            return false;
        }
    }

    public class JSCompiler
    {
        public static dynamic Run(string code, string eval, NameContext context)
        {
            var compiler = new JSCompiler();
            var byteCode = compiler.Parse(code);
            var exitState = byteCode.Run(context);
            if (eval != null)
            {
                var a = context.GetMember(eval);
                return a != null ? a.ScalarValue : null;
            }
            return null;
        }

        public CodePiece Parse(string code)
        {
            var p = new JSReader(code);
            var block = new CodeBlock();
            ReadCodes(p, ref block);
            return block;
        }

        private void Expect(StringReader p, bool truth, string msg = "expected")
        {
            if (!truth)
                throw new Exception(msg + "\n" + p.RestOf);
        }

        private static string[] reservedWords = { "break", "case", "catch", "continue", "debugger", "default", "delete", "do", 
            "else", "finally", "for", "function", "if", "in", "instanceof", "new", "return", "switch", "throw", "try", 
            "typeof", "var", "void", "while", "with" };

        private bool ReadIdent(StringReader p, out string ident)
        {
            var saved = p.Position;
            string wordRead;
            if (p.AnyWord(out wordRead))
            {
                var isRW = reservedWords.Any(w => w == wordRead);
                if (!isRW)
                {
                    ident = wordRead;
                    return true;
                }
                p.Position = saved;
            }
            ident = null;
            return false;
        }

        private bool ReadCodes(StringReader p, ref CodeBlock block)
        {
            var readCount = 0;
            while (ReadCode(p, ref block))
                readCount++;
            return readCount > 0;
        }

        private bool ReadCode(StringReader p, ref CodeBlock block)
        {
            var lret = ReadBlock(p, ref block) || ReadIf(p, ref block) || ReadWhile(p, ref block) ||
                ReadDo(p, ref block) || ReadFunctionDecl(p, ref block) || ReadDelete(p, ref block) || ReadTry(p, ref block) ||
                ReadVar(p, ref block) || ReadForEach(p, ref block) || ReadFor(p, ref block) || ReadSwitch(p, ref block) ||
                ReadBreak(p, ref block) || ReadReturn(p, ref block) || ReadContinue(p, ref block) || ReadCall(p, ref block);
            if (lret)
                p.ThisText(";");
            return lret;
        }

        private void AddCode(ref CodeBlock block, CodePiece code, bool insert = false)
        {
            if (block == null)
                block = new CodeBlock();
            if (block.Codes == null)
                block.Codes = new List<CodePiece>();
            if (insert)
                block.Codes.Insert(0, code);
            else
                block.Codes.Add(code);
        }

        private bool ReadCall(StringReader p, ref CodeBlock block)
        {
            ExprPiece value;
            var saved = p.Position;
            if (ReadValue(p, out value))
            {
                if (value.IsCallable)
                {
                    AddCode(ref block, new CallCode { Arg = value });
                    return true;
                }
                else
                    p.Position = saved;
            }
            return false;
        }

        private void ReqText(StringReader p, string text)
        {
            if (!p.ThisText(text))
                throw new Exception("expected '" + text + "'" + "\n" + p.RestOf);
        }

        private void ReqVal(StringReader p, out ExprPiece val)
        {
            if (!ReadValue(p, out val))
                throw new Exception("value expected" + "\n" + p.RestOf);
        }

        private bool ReadTry(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("try"))
            {
                var jstry = new TryCode();
                ReqText(p, "{");
                ReadCodes(p, ref jstry.Code);
                ReqText(p, "}");
                int catchCount = 0;
                int finallyCount = 0;
                if (p.ThisWord("catch"))
                {
                    ReqText(p, "(");
                    ReadIdent(p, out jstry.CatchName);
                    ReqText(p, ")");
                    ReqText(p, "{");
                    ReadCodes(p, ref jstry.Catch);
                    ReqText(p, "}");
                    catchCount++;
                }
                if (p.ThisWord("finally"))
                {
                    ReqText(p, "{");
                    ReadCodes(p, ref jstry.Finally);
                    ReqText(p, "}");
                    finallyCount++;
                }
                Expect(p, catchCount > 0 || finallyCount > 0, "catch/finally");
                AddCode(ref block, jstry);
                return true;
            }
            return false;
        }

        private bool ReadSwitch(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("switch"))
            {
                var jsswitch = new SwitchCode();
                ReqText(p, "(");
                ReqVal(p, out jsswitch.Arg);
                ReqText(p, ")");
                ReqText(p, "{");
                while (ReadCase(p, jsswitch)) ;
                if (p.ThisWord("default"))
                {
                    ReqText(p, ":");
                    ReadCodes(p, ref jsswitch.Default);
                }
                ReqText(p, "}");
                AddCode(ref block, jsswitch);
                return true;
            }
            return false;
        }

        private bool ReadCase(StringReader p, SwitchCode jsswitch)
        {
            if (p.ThisWord("case"))
            {
                var jscase = new SwitchCaseCode();
                ReqVal(p, out jscase.Test);
                ReqText(p, ":");
                ReadCodes(p, ref jscase.Code);
                (jsswitch.Cases ?? (jsswitch.Cases = new List<SwitchCaseCode>())).Add(jscase);
                return true;
            }
            return false;
        }

        private bool ReadVar(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("var"))
            {
            ANOTHER:
                var jsvar = new VarCode();
                Expect(p, ReadIdent(p, out jsvar.Name), "var name");
                if (p.ThisText("="))
                    ReqVal(p, out jsvar.Value);
                AddCode(ref block, jsvar);
                if (p.ThisText(","))
                    goto ANOTHER;
                return true;
            }
            return false;
        }

        private bool ReadDelete(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("delete"))
            {
                ExprPiece objRef;
                ReqVal(p, out objRef);
                Expect(p, objRef is DotExpr || objRef is NamedExpr || objRef is IndexExpr, "delete expr");
                var jsdel = new DeleteCode();
                jsdel.ObjectRef = objRef;
                AddCode(ref block, jsdel);
                return true;
            }
            return false;
        }

        private bool ReadFunctionDecl(StringReader p, ref CodeBlock block)
        {
            FunctionCode funcRead;
            if (ReadFunctionBody(p, true, out funcRead))
            {
                AddCode(ref block, funcRead, true);
                return true;
            }
            return false;
        }

        private bool ReadFunctionBody(StringReader p, bool reqName, out FunctionCode func)
        {
            var saved = p.Position;
            if (p.ThisWord("function"))
            {
                var jsfn = new FunctionCode();
                // esse Reader pode ser chamado para declaracao de funcoes ou para expressoes de funcoes anonimas
                // reqName diferencia esses dois usos, um tem nome o outro nao
                // ex.: function X(){} ou function(){}
                if (reqName)
                    Expect(p, ReadIdent(p, out jsfn.Name), "func name");
                ReqText(p, "(");
                var plist = new List<string>();
                string pn;
                while (ReadIdent(p, out pn))
                {
                    plist.Add(pn);
                    if (!p.ThisText(","))
                        break;
                }
                jsfn.Params = plist.Count > 0 ? plist.ToArray() : null;
                ReqText(p, ")");
                ReqText(p, "{");
                ReadCodes(p, ref jsfn.Code);
                ReqText(p, "}");
                func = jsfn;
                return true;
            }
            func = null;
            return false;
        }

        private bool ReadClosure(StringReader p, out FunctionCode func)
        {
            var saved = p.Position;
            var plist = new List<string>();
            string pn;
            if (p.ThisText("("))
            {
                while (ReadIdent(p, out pn))
                {
                    plist.Add(pn);
                    if (!p.ThisText(","))
                        break;
                }
                if (!p.ThisText(")"))
                    goto Mistaken;
            }
            else if (ReadIdent(p, out pn))
                plist.Add(pn);
            if (p.ThisText(":"))
            {
                var jsfn = new FunctionCode();
                jsfn.Params = plist.Count > 0 ? plist.ToArray() : null;
                if (p.ThisText("{"))
                {
                    ReadCodes(p, ref jsfn.Code);
                    ReqText(p, "}");
                }
                else
                {
                    ExprPiece arg;
                    ReqVal(p, out arg);
                    AddCode(ref jsfn.Code, new ReturnCode { Arg = arg });
                }
                func = jsfn;
                return true;
            }
        Mistaken:
            p.Position = saved;
            func = null;
            return false;
        }

        private bool ReadForEach(StringReader p, ref CodeBlock block)
        {
            var saved = p.Position;
            if (p.ThisWord("for"))
            {
                var jsfor = new ForeachCode();
                ReqText(p, "(");
                if (p.ThisWord("var"))
                    if (ReadIdent(p, out jsfor.VarName))
                        if (p.ThisWord("in"))
                        {
                            ReqVal(p, out jsfor.SetExpr);
                            ReqText(p, ")");
                            ReadCode(p, ref jsfor.Code);
                            AddCode(ref block, jsfor);
                            return true;
                        }
                p.Position = saved;
            }
            return false;
        }

        private bool ReadFor(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("for"))
            {
                var jsfor = new ForCode();
                ReqText(p, "(");
                ReadForInitialization(p, jsfor);
                ReqText(p, ";");
                ReadForCondition(p, jsfor);
                ReqText(p, ";");
                ReadForIncrement(p, jsfor);
                ReqText(p, ")");
                ReadCode(p, ref jsfor.Code);
                AddCode(ref block, jsfor);
                return true;
            }
            return false;
        }

        private bool ReadForInitialization(StringReader p, ForCode jsfor)
        {
            CodeBlock foo = null;
            if (ReadVar(p, ref foo))
            {
                var vardecl = (VarCode)foo.Codes.First();
                Expect(p, vardecl.Value != null, "for var");
                jsfor.Initialization = vardecl;
                return true;
            }
            else if (ReadCall(p, ref foo))
            {
                var init = (CallCode)foo.Codes.First();
                Expect(p, init.Arg is AssignExpr, "for assign");
                jsfor.Initialization = init;
                return true;
            }
            else
                return false;
        }

        private bool ReadForCondition(StringReader p, ForCode jsfor)
        {
            return ReadValue(p, out jsfor.Condition);
        }

        private bool ReadForIncrement(StringReader p, ForCode jsfor)
        {
            CodeBlock foo = null;
            if (ReadCall(p, ref foo))
            {
                var incr = (CallCode)foo.Codes.First();
                Expect(p, incr.Arg is OperationExpr, "for incr arg");
                jsfor.Increment = incr;
                return true;
            }
            else
                return false;
        }

        private bool ReadBreak(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("break"))
            {
                AddCode(ref block, new BreakCode());
                return true;
            }
            return false;
        }

        private bool ReadContinue(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("continue"))
            {
                AddCode(ref block, new ContinueCode());
                return true;
            }
            return false;
        }

        private bool ReadReturn(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("return"))
            {
                var jsret = new ReturnCode();
                ReadValue(p, out jsret.Arg);
                AddCode(ref block, jsret);
                return true;
            }
            return false;
        }

        private bool ReadDo(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("do"))
            {
                var jsdo = new UntilCode();
                ReqText(p, "{");
                ReadCodes(p, ref jsdo.Code);
                ReqText(p, "}");
                Expect(p, p.ThisWord("while"), "while");
                ReqText(p, "(");
                ReqVal(p, out jsdo.Condition);
                ReqText(p, ")");
                AddCode(ref block, jsdo);
                return true;
            }
            return false;
        }

        private bool ReadWhile(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("while"))
            {
                var jswhile = new WhileCode();
                ReqText(p, "(");
                ReqVal(p, out jswhile.Condition);
                ReqText(p, ")");
                ReadCode(p, ref jswhile.Code);
                AddCode(ref block, jswhile);
                return true;
            }
            return false;
        }

        private bool ReadIf(StringReader p, ref CodeBlock block)
        {
            if (p.ThisWord("if"))
            {
                var jsif = new IfCode();
                ReqText(p, "(");
                ReqVal(p, out jsif.Condition);
                ReqText(p, ")");
                ReadCode(p, ref jsif.Then);
                if (p.ThisWord("else"))
                    ReadCode(p, ref jsif.Else);
                AddCode(ref block, jsif);
                return true;
            }
            return false;
        }

        private bool ReadBlock(StringReader p, ref CodeBlock block)
        {
            if (p.ThisText("{"))
            {
                ReadCodes(p, ref block);
                ReqText(p, "}");
                return true;
            }
            return false;
        }

        private static List<Type> OperatorPrecedence = new List<Type>(
        new[]{
            typeof(PreIncExpr), typeof(PreDecExpr), typeof(PosIncExpr), typeof(PosDecExpr), typeof(BoolNotExpr), typeof(BitNotExpr),
            typeof(PlusExpr), typeof(NegExpr),
            typeof(TypeOfExpr),
            typeof(MultiplyExpr), typeof(DivideExpr), typeof(ModulusExpr), typeof(SumExpr), typeof(SubtractExpr),
            typeof(ShlExpr), typeof(ShrExpr), typeof(ShrExExpr),
            typeof(LtExpr), typeof(LtEqExpr), typeof(GtExpr), typeof(GtEqExpr),
            typeof(InstanceOfExpr),
            typeof(EqExpr), typeof(NotEqExpr), typeof(ExactEqExpr), typeof(NotExactEqExpr),
            typeof(BitAndExpr), typeof(BitXorExpr), typeof(BitOrExpr),
            typeof(BoolAndExpr), typeof(BoolOrExpr),
            typeof(ConditionalExpr),
            typeof(AssignExpr),
            typeof(SelfSumExpr), typeof(SelfSubtractExpr), typeof(SelfMultiplyExpr), typeof(SelfDivideExpr),
            typeof(SelfModulusExpr),
            typeof(SelfShlExpr), typeof(SelfShrExpr), typeof(SelfShrExExpr),
            typeof(SelfBitAndExpr), typeof(SelfBitXorExpr), typeof(SelfBitOrExpr),
            typeof(SelfBoolAndExpr), typeof(SelfBoolOrExpr),            
        });

        private bool ReadValueItem(StringReader p, List<object> mixture)
        {
            ExprPiece vaux;
            OperationExpr op;
            // valor simples, unario+valor+unario
            if (ReadSimpleValue(p, out vaux))
                mixture.Add(vaux);
            else if (ReadUnaryPreOp(p, out op))
            {
                mixture.Add(op);
                Expect(p, ReadSimpleValue(p, out vaux), "simple");
                mixture.Add(vaux);
            }
            else
                return false;
            if (ReadUnaryPosOp(p, out op))
                mixture.Add(op);
            return true;
        }

        private bool ReadValue(StringReader p, out ExprPiece value)
        {
            OperationExpr op;
            var mixture = new List<object>();
            if (!ReadValueItem(p, mixture))
            {
                value = null;
                return false;
            }
            while (ReadBinaryOp(p, out op))
            {
                mixture.Add(op);
                Expect(p, ReadValueItem(p, mixture), "mix item");
            }
            value = SplitOperands(mixture, 0, mixture.Count);
            return true;
        }

        private ExprPiece SplitOperands(List<object> mixture, int start, int end)
        {
            if (mixture == null || mixture.Count == 0)
                return null;
            var maxPrio = 0;
            var iPrio = -1;
            for (var i = start; i < end; i++)
                if (mixture[i] is OperationExpr)
                {
                    var op = (OperationExpr)mixture[i];
                    var prio = OperatorPrecedence.IndexOf(op.GetType());
                    if (iPrio == -1 || prio >= maxPrio)
                    {
                        maxPrio = prio;
                        iPrio = i;
                    }
                }
            // nao ha operadores, soh uma expressao simples
            if (iPrio == -1)
                return (ExprPiece)mixture[start];
            // existe um operador bem no inicio, ele eh um operador unario seguido de uma expressao
            if (iPrio == start)
                if (end - start == 2)
                {
                    var unop = (UnaryExpr)mixture[start];
                    unop.Arg = (ExprPiece)mixture[start + 1];
                    return unop;
                }
                else
                    throw new Exception("no value");
            // existe um operador bem no final, deve ser um pos incremento ou decremento, precedido de uma expressao
            if (iPrio == end - 1)
                if (end - start == 2)
                {
                    var unop = (UnaryExpr)mixture[end - 1];
                    unop.Arg = (ExprPiece)mixture[start];
                    return unop;
                }
                else
                    throw new Exception("no value");
            var value = (BinaryExpr)mixture[iPrio];
            value.LeftArg = SplitOperands(mixture, start, iPrio);
            value.RightArg = SplitOperands(mixture, iPrio + 1, end);
            return value;
        }

        private bool ReadUnaryPreOp(StringReader p, out OperationExpr op)
        {
            op = null;
            int i;
            if (p.ThisText(new[] { "++", "--", "+", "-", "!", "~" }, out i))
            {
                if (i == 0)
                    op = new PreIncExpr();
                else if (i == 1)
                    op = new PreDecExpr();
                else if (i == 2)
                    op = new PlusExpr();
                else if (i == 3)
                    op = new NegExpr();
                else if (i == 4)
                    op = new BoolNotExpr();
                else if (i == 5)
                    op = new BitNotExpr();
            }
            return op != null;
        }

        private bool ReadUnaryPosOp(StringReader p, out OperationExpr op)
        {
            op = null;
            int i;
            if (p.ThisText(new[] { "++", "--" }, out i))
                if (i == 0)
                    op = new PosIncExpr();
                else if (i == 1)
                    op = new PosDecExpr();
            return op != null;
        }

        private static string[] binst = {
                ">>>=",
                "===", ">>>", ">>=", "<<=", "!==",
                ">=", "<=", "==", "!=", "<<", ">>", "&&", "||", "^=", "&=", "|=", "/=", "*=", "+=", "-=", "%=",
                ">", "<", "=", "+", "-", "*", "/", "%", "&", "|", "^" };
        private static Type[] binop = { 
                    typeof(SelfShrExExpr),
                    typeof(ExactEqExpr), typeof(ShrExExpr), typeof(SelfShrExpr),typeof(SelfShlExpr),typeof(NotExactEqExpr),
                    typeof(GtEqExpr),typeof(LtEqExpr),typeof(EqExpr),typeof(NotEqExpr),typeof(ShlExpr),typeof(ShrExpr),
                    typeof(BoolAndExpr),typeof(BoolOrExpr),typeof(SelfBitXorExpr),typeof(SelfBitAndExpr),typeof(SelfBitOrExpr),
                    typeof(SelfDivideExpr),typeof(SelfMultiplyExpr),typeof(SelfSumExpr),typeof(SelfSubtractExpr),typeof(SelfModulusExpr),
                    typeof(GtExpr),typeof(LtExpr),typeof(AssignExpr),typeof(SumExpr),typeof(SubtractExpr),typeof(MultiplyExpr),
                    typeof(DivideExpr),typeof(ModulusExpr),typeof(BitAndExpr),typeof(BitOrExpr),typeof(BitXorExpr),
                                            };
        private bool ReadBinaryOp(StringReader p, out OperationExpr bin)
        {
            bin = null;
            int i;
            if (p.ThisText(binst, out i))
            {
                bin = (OperationExpr)Activator.CreateInstance(binop[i]);
                return true;
            }
            return false;
        }

        private bool ReadSimpleValue(StringReader p, out ExprPiece value)
        {
            if (ReadFunctionValue(p, out value) || ReadSymbolValue(p, out value) || ReadNewValue(p, out value) ||
                ReadNamedValue(p, out value) || ReadFloatValue(p, out value) || ReadStringValue(p, out value) ||
                ReadParensValue(p, out value) || ReadArrayValue(p, out value) || ReadObjectValue(p, out value))
            {
                while (ReadMemberValue(p, ref value)) ;
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadNewValue(StringReader p, out ExprPiece value)
        {
            if (p.ThisWord("new"))
            {
                ExprPiece creator;
                Expect(p, ReadNamedValue(p, out creator) || ReadFloatValue(p, out creator) || ReadStringValue(p, out creator) ||
                    ReadParensValue(p, out creator) || ReadArrayValue(p, out creator) || ReadObjectValue(p, out creator), "new arg");
                while (ReadMemberValue(p, ref creator))
                    if (creator is ParamsExpr)
                        break;
                value = new NewExpr { Creator = creator };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadValuePair(StringReader p, ObjectExpr obj)
        {
            var saved = p.Position;
            string pairName;
            if (ReadIdent(p, out pairName) || p.AnyQuoted(out pairName))
            {
                if (p.ThisText(":"))
                {
                    ExprPiece pairValue;
                    ReqVal(p, out pairValue);
                    (obj.Pairs ?? (obj.Pairs = new Dictionary<string, ExprPiece>())).Add(pairName, pairValue);
                    return true;
                }
                p.Position = saved;
            }
            return false;
        }

        private bool ReadObjectValue(StringReader p, out ExprPiece value)
        {
            if (p.ThisText("{"))
            {
                var obj = new ObjectExpr();
                while (ReadValuePair(p, obj))
                    if (!p.ThisText(","))
                        break;
                ReqText(p, "}");
                value = obj;
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadArrayValue(StringReader p, out ExprPiece value)
        {
            if (p.ThisText("["))
            {
                var list = new List<ExprPiece>();
                ExprPiece item;
                while (ReadValue(p, out item))
                {
                    list.Add(item);
                    if (!p.ThisText(","))
                        break;
                }
                ReqText(p, "]");
                value = new ArrayExpr { Items = list.ToArray() };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadFunctionValue(StringReader p, out ExprPiece value)
        {
            FunctionCode funcRead;
            if (ReadFunctionBody(p, false, out funcRead) || ReadClosure(p, out funcRead))
            {
                value = new FunctionExpr { Function = funcRead };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadMemberValue(StringReader p, ref ExprPiece value)
        {
            return ReadDotMember(p, ref value) || ReadIndexMember(p, ref value) || ReadParamsMember(p, ref value);
        }

        private bool ReadParamsMember(StringReader p, ref ExprPiece value)
        {
            ParamsExpr param;
            if (ReadParamsExpr(p, out param))
            {
                param.FuncExpr = value;
                value = param;
                return true;
            }
            return false;
        }

        private bool ReadIndexMember(StringReader p, ref ExprPiece value)
        {
            if (p.ThisText("["))
            {
                var index = new IndexExpr();
                index.Array = value;
                ReqVal(p, out index.Index);
                ReqText(p, "]");
                value = index;
                return true;
            }
            return false;
        }

        private bool ReadParensValue(StringReader p, out ExprPiece value)
        {
            if (p.ThisText("("))
            {
                var parens = new ParensExpr();
                ReadValue(p, out parens.InnerExpr);
                ReqText(p, ")");
                value = parens;
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadDotMember(StringReader p, ref ExprPiece value)
        {
            var saved = p.Position;
            if (p.ThisText("."))
            {
                string nameRead;
                if (ReadIdent(p, out nameRead))
                {
                    value = new DotExpr { MemberName = nameRead, LeftArg = value };
                    return true;
                }
                p.Position = saved;
            }
            return false;
        }

        private bool ReadStringValue(StringReader p, out ExprPiece value)
        {
            string s;
            if (p.AnyQuoted(out s))
            {
                value = new LiteralConst { Literal = s };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadParamsExpr(StringReader p, out ParamsExpr value)
        {
            if (p.ThisText("("))
            {
                var list = new List<ExprPiece>();
                ExprPiece paramRead;
                while (ReadValue(p, out paramRead))
                {
                    list.Add(paramRead);
                    if (!p.ThisText(","))
                        break;
                }
                ReqText(p, ")");
                value = new ParamsExpr { Params = list.ToArray() };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadFloatValue(StringReader p, out ExprPiece value)
        {
            var saved = p.Position;
            p.Skip();
            string f;
            // sinal
            char sig;
            if (p.ThisCharNoSkip("+-", out sig))
                f = sig.ToString();
            else
                f = "";
            bool hasint = false, hasfrac = false, hasexp = false;
            // int part
            string intpart;
            if (p.ThisSetNoSkip("0123456789", out intpart))
            {
                f += intpart;
                hasint = true;
            }
            else
                f += "0";
            var befpt = p.Position;
            // frac part
            if (p.ThisCharNoSkip('.'))
            {
                string fracpart;
                if (p.ThisSetNoSkip("0123456789", out fracpart))
                {
                    f += "." + fracpart;
                    hasfrac = true;
                }
                if (!hasfrac)
                    p.Position = befpt;
            }
            // exp part
            if (hasint || hasfrac)
            {
                var befexp = p.Position;
                if (p.ThisCharNoSkip("eE"))
                {
                    char expsig;
                    if (p.ThisCharNoSkip("+-", out expsig))
                    {
                        string exppart;
                        if (p.ThisSetNoSkip("0123456789", out exppart))
                        {
                            f += "e" + expsig.ToString() + exppart;
                            hasexp = true;
                        }
                    }
                    if (!hasexp)
                        p.Position = befexp;
                }

                var lit = new LiteralConst();
                int ival;
                if (hasfrac)
                {
                    var comma = 1.1.ToString()[1];
                    f = f.Replace('.', comma);
                    lit.Literal = double.Parse(f);
                }
                else if (int.TryParse(f, out ival))
                    lit.Literal = ival;
                else
                    lit.Literal = long.Parse(f);
                value = lit;
                return true;
            }
            p.Position = saved;
            value = null;
            return false;
        }

        private bool ReadNamedValue(StringReader p, out ExprPiece value)
        {
            string aux;
            if (ReadIdent(p, out aux))
            {
                value = new NamedExpr { Name = aux };
                return true;
            }
            value = null;
            return false;
        }

        private bool ReadSymbolValue(StringReader p, out ExprPiece value)
        {
            int wordRead;
            if (p.ThisWord(new[] { "null", "true", "false" }, out wordRead))
                switch (wordRead)
                {
                    case 0: value = new LiteralConst { Literal = null }; return true;
                    case 1: value = new LiteralConst { Literal = true }; return true;
                    case 2: value = new LiteralConst { Literal = false }; return true;
                }
            value = null;
            return false;
        }
    }
}
