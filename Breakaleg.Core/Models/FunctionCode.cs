using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class FunctionCode : CodePiece
    {
        public string Name;
        public string[] Params;
        public CodeBlock Code;
        public NativeCodeCallback NativeCode;

        public FunctionCode() { }

        public FunctionCode(NativeCodeCallback native)
        {
            this.NativeCode = native;
        }

        public override ExitResult Run(NameContext context)
        {
            context.SetField(this.Name, Instance.DefineType(this, context));
            return null;
        }

        public ExitResult Call(NameContext context, Instance owner, Instance[] args)
        {
            if (NativeCode != null)
                return new ExitResult
                {
                    ExitMode = Models.ExitMode.Normal,
                    ExitValue = NativeCode(owner, args)
                };
            // nesse callContext serao guardados os parametros e o simbolo this=owner
            var callContext = context.NewChild();
            if (Params != null)
                for (var i = 0; i < Params.Length; i++)
                    callContext.SetField(Params[i], args != null && i < args.Length ? args[i] : null);
            if (owner != null)
                callContext.SetField("this", owner);
            return Code.Run(callContext);
        }

        public override string ToString()
        {
            return string.Format("FUNC({0})", Name);
        }
    }
}