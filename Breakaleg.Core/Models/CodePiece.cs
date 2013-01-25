using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public abstract class CodePiece
    {
        public abstract ExitResult Run(NameContext context);

        public override string ToString()
        {
            return this.GetType().Name.Replace("Code", "");
        }
    }
}