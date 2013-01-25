namespace Breakaleg.Core.Compiler
{
    public class TextPosition
    {
        public int CharIndex;
        public int LineNo;
        public int ColNo;
        internal int LineStart;

        public override string ToString()
        {
            return LineNo + ":" + ColNo;
        }
    }
}