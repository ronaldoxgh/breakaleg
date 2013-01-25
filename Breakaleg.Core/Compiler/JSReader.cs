namespace Breakaleg.Core.Compiler
{
    public class JSReader : StringReader
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
}
