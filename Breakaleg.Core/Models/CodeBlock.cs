using System.Collections.Generic;
using System.Linq;
using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public class CodeBlock : CodePiece
    {
        public List<CodePiece> Codes;

        public override ExitResult Run(NameContext context)
        {
            if (Codes == null || Codes.Count == 0)
                return null;
            return Codes.Select(code => code.Run(context)).FirstOrDefault(result => result != null && result.ExitMode != ExitMode.Normal);
        }

        public override string ToString()
        {
            return "BLOCK(...)";
        }
    }
}