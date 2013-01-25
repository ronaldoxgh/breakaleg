using Breakaleg.Core.Dynamic;

namespace Breakaleg.Core.Models
{
    public enum ExitMode { Normal, Break, Return, Continue, Except }

    public class ExitResult
    {
        public ExitMode ExitMode;
        public Instance ExitValue;
    }
}