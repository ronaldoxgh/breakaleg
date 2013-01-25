using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
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

        public override string ToString()
        {
            return "TRY(...)";
        }
    }
}