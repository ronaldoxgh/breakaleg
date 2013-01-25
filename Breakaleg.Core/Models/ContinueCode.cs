using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class ContinueCode : CodePiece
    {
        public override ExitResult Run(NameContext context)
        {
            return new ExitResult
            {
                ExitMode = ExitMode.Continue
            };
        }
    }
}