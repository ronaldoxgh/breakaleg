using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Breakaleg.Core.Compilers;
using Breakaleg.Core.Models;

namespace Breakaleg.Tests
{
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1() { }

        private TestContext testContextInstance;

        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

        dynamic Run(string code, string ret)
        {
            var p = new JSCompiler();
            var c = p.Parse(code);
            var ctx = new NameContext();
            c.Run(ctx);
            if (ret != null)
            {
                var a = ctx.GetMember(ret);
                return a != null ? a.ScalarValue : null;
            }
            return null;
        }

        [TestMethod]
        public void TestMethod_Context()
        {
            Assert.AreEqual(1, Run("a=1", "a"));
            Assert.AreEqual(1, Run("var a=1;", "a"));
            Assert.IsNull(Run("a=1", "b"));
        }

        [TestMethod]
        public void TestMethod_SelfInc()
        {
            Assert.AreEqual(1, Run("a=0;++a;", "a"));
            Assert.AreEqual(2, Run("a=1;++a;", "a"));
            Assert.AreEqual(3, Run("a=2;a++;", "a"));
        }

        [TestMethod]
        public void TestMethod_SelfOp()
        {
            Assert.AreEqual(15, Run("a=5;a+=10;", "a"));
            Assert.AreEqual(6, Run("a=1;a+=8;a*=2;a/=3;", "a"));
            Assert.AreEqual(2, Run("a=5;a%=3;", "a"));
        }

        [TestMethod]
        public void TestMethod_If()
        {
            Assert.AreEqual(1, Run("a=0;if(1==1)a=1;else a=2;", "a"));
            Assert.AreEqual(2, Run("a=0;if(1>1)a=1;else a=2;", "a"));
        }

        [TestMethod]
        public void TestMethod_While()
        {
            Assert.AreEqual(3, Run("a=0;while(a<3)a=a+1;", "a"));
        }

        [TestMethod]
        public void TestMethod_For()
        {
            Run("for(var i=0;i<3;i++);", null);
            Assert.AreEqual(31, Run("a=1;for(i=0;i<30;i++)a++;", "a"));
        }

        [TestMethod]
        public void TestMethod_Switch()
        {
            var code = @"
                s='none';
                switch(#){
                case 1:s='case1';break;
                case 2:s='case2';break;
                default:s='default';break;
                }
                ";
            Assert.AreEqual("case1", Run(code.Replace('#', '1'), "s"));
            Assert.AreEqual("case2", Run(code.Replace('#', '2'), "s"));
            Assert.AreEqual("default", Run(code.Replace('#', '4'), "s"));
        }

        [TestMethod]
        public void TestMethod_Function()
        {
            var code1 = @"function Vai(s){return s+s;}; result=Vai('t')";
            Assert.AreEqual("tt", Run(code1, "result"));

            var code2 = @"result=Vai('t'); function Vai(s){return s+1+s;}";
            Assert.AreEqual("t1t", Run(code2, "result"));
        }

        [TestMethod]
        public void TestMethod_FunctionInFunction()
        {
            var code = @"
                function Vai(s)
                {
                    function Bacana(s2)
                    {
                        s+='y';
                        return 'B'+s2+s;
                    }
                    return 'V'+Bacana(s)+s;
                };
                result=Vai('t')";
            Assert.AreEqual("VBttyty", Run(code, "result"));
        }

        [TestMethod]
        public void TestMethod_Array()
        {
            string code;
            code = "a=[1,2,3]; result=a.length";
            Assert.AreEqual(3, Run(code, "result"));

            code = "a=[1,2,3]; result=a[2]";
            Assert.AreEqual(3, Run(code, "result"));
        }

        [TestMethod]
        public void TestMethod_Object()
        {
            string code;

            code = "a={nome:'ze',idade:32,peso:50.5}; result=a.idade";
            Assert.AreEqual(32, Run(code, "result"));

            code = "a={nome:'ze',idade:32,peso:50.5}; result=a.nome";
            Assert.AreEqual("ze", Run(code, "result"));

            code = "a={}; a.nome='ze'; a.idade=32; a.peso=50.5; result=a.nome";
            Assert.AreEqual("ze", Run(code, "result"));

            code = "a={nome:'ze',idade:32}; a.peso=50.5; result=a.nome+a.peso";
            Assert.AreEqual("ze50,5", Run(code, "result"));

            code = "a={nome:'ze',filhos:['jo','kim','ni']}; result=a.filhos[1]";
            Assert.AreEqual("kim", Run(code, "result"));

            code = "a={nome:'ze','idade':32}; r1=a['nome']; r2=a.idade; a.idade='no'+'me'; r3=a[a.idade];";
            Assert.AreEqual("ze", Run(code, "r1"));
            Assert.AreEqual(32, Run(code, "r2"));
            Assert.AreEqual("ze", Run(code, "r3"));
        }

        [TestMethod]
        public void TestMethod_Delete()
        {
            string code;

            code = "a={nome:'ze',idade:32,peso:50}; antes=a.nome; delete a.nome; result=a.nome";
            Assert.AreEqual("ze", Run(code, "antes"));
            Assert.IsNull(Run(code, "result"));
        }

        [TestMethod]
        public void TestMethod_This()
        {
            string code = @"
                function Pessoa(n)
                {
                    var nada=1;
                    this.Nome=n;
                };
                var p=new Pessoa('xico');
                r1=p.Nome;
                r2=p.nada;
                ";
            Assert.AreEqual("xico", Run(code, "r1"));
            Assert.IsNull(Run(code, "r2"));
        }

        [TestMethod]
        public void TestMethod_FunctionExpr()
        {
            string code = @"
                var f=function(p){return 'bill'+p};
                result=f('gates');
                ";
            Assert.AreEqual("billgates", Run(code, "result"));
        }

        [TestMethod]
        public void TestMethod_Method()
        {
            string code = @"
                function Pessoa()
                {
                    var nome='dexter';
                    this.GetConst=function(){return 123};
                    this.GetNome=function(){return nome};
                };
                var p=new Pessoa();
                r1=p.GetConst();
                r2=p.GetNome();
                ";
            Assert.AreEqual(123, Run(code, "r1"));
            Assert.AreEqual("dexter", Run(code, "r2"));
        }

        [TestMethod]
        public void TestMethod_New()
        {
            string code = @"
                function Pessoa(n)
                {
                    var priv=n;
                    this.Nome=n;
                    this.Get=function(){return priv};
                };
                var p1=new Pessoa('xico');
                var p2=new Pessoa('beto');
                result=p1.Get()+'.'+p2.Nome;
                ";
            Assert.AreEqual("xico.beto", Run(code, "result"));
        }

        [TestMethod]
        public void TestMethod_PrivatePerInstance()
        {
            string code = @"
                function Pessoa(n)
                {
                    var priv=n;
                    this.Get=function(){return priv};
                    this.Set=function(v){priv=v};
                };

                var p1=new Pessoa('xico');
                r1a=p1.Get();
                p1.Set('jona');
                r1b=p1.Get();

                var p2=new Pessoa('beto');
                r2a=p2.Get();
                p2.Set('mary');
                r2b=p2.Get();

                r1c=p1.Get();
                ";
            Assert.AreEqual("xico", Run(code, "r1a"));
            Assert.AreEqual("jona", Run(code, "r1b"));
            Assert.AreEqual("jona", Run(code, "r1c"));
            Assert.AreEqual("beto", Run(code, "r2a"));
            Assert.AreEqual("mary", Run(code, "r2b"));
        }

        class JSNS
        {
            public int a;
            public int b;

            public void alert(dynamic s)
            {
                Console.WriteLine(s);
            }

            public dynamic Soma(dynamic p1, dynamic p2)
            {
                return p1 + p2;
            }
        }

        [TestMethod]
        public void TestMethod_SolveExternal()
        {
            var code = @"
                r=a+b*3;
                alert(r);
                x=Soma(4,5);
                ";
            var ctx = new NameContext();
            ctx.AddNamespace(new JSNS { a = 10, b = 30 });
            var ret = JSCompiler.Run(code, "r", ctx);
            Assert.AreEqual(100, ret);
            ret = JSCompiler.Run(code, "x", ctx);
            Assert.AreEqual(9, ret);
        }

        [TestMethod]
        public void TestMethod_Closures()
        {
            /*
            a:1+2;
            a:=1+2;
            :{1+2};
            :1;
            */

            Assert.AreEqual(14, JSCompiler.Run("f=a:7*a; r=f(2)", "r", new NameContext()));
            Assert.AreEqual(6, JSCompiler.Run("var f=(a,b):a*b; r=f(2,3)", "r", new NameContext()));
            Assert.AreEqual(545, JSCompiler.Run("r=(a:543+a)(2)", "r", new NameContext()));
            Assert.AreEqual(24, JSCompiler.Run("f=a:{return 8*a}; r=f(3)", "r", new NameContext()));
            Assert.AreEqual(40, JSCompiler.Run("f=:8*5; r=f()", "r", new NameContext()));
        }

        [TestMethod]
        public void TestMethod_MethodsAsClosures()
        {
            var code = @"var obj = {
              firstName:'john', lastName:'doe',
              getFullName: :this.firstName+' '+this.lastName,
            };
            obj.lastName='nash';
            r=obj.getFullName();
            ";
            Assert.AreEqual("john nash", JSCompiler.Run(code, "r", new NameContext()));
        }

        [TestMethod]
        public void TestMethod_Prototype()
        {
            var code = @"
                function Person(){
                    this.Name='zacaro';
                    this.Age= :123;
                }
                var p=new Person();
                n=p.Name;
                a=p.Age();
                Person.prototype.What=:34;
                var p2=new Person();
                x=p2.What();
                ";
            Assert.AreEqual("zacaro", JSCompiler.Run(code, "n", new NameContext()));
            Assert.AreEqual(123, JSCompiler.Run(code, "a", new NameContext()));
            Assert.AreEqual(34, JSCompiler.Run(code, "x", new NameContext()));
        }

        [TestMethod]
        public void TestMethod_ForEach()
        {
            Assert.AreEqual(7, Run("t=0;for(var n in [1,2,3])t+=n;", "t"));
        }
    }
}
