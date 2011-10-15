using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Breakaleg.Core.Readers
{
    public class TextPosition
    {
        public int CharIndex;
        public int LineNo;
        public int ColNo;
        internal int _lineStart;

        public override string ToString()
        {
            return LineNo + ":" + ColNo;
        }
    }

    ///TODO case-sensitiveness
    public class StringReader
    {
        public const string AlphaCharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";
        public const string DigitCharSet = "0123456789";
        public const string AlphaCharSetEx = AlphaCharSet + DigitCharSet;
        public const string SpaceCharSet = "\x0d\x0a\x20\x09\x1a";
        public const string HexCharSet = "0123456789abcdefABCDEF";

        private char[] _chars;
        private TextPosition _pos = new TextPosition();

        public StringReader(string sourceCode)
        {
            _chars = sourceCode.ToCharArray();
            _pos = new TextPosition { CharIndex = 0, LineNo = 1, ColNo = 1 };
        }

        public string CurrentLine { get { return new string(_chars, _pos._lineStart, _pos.CharIndex - _pos._lineStart); } }

        public TextPosition Position
        {
            get
            {
                return new TextPosition { CharIndex = _pos.CharIndex, ColNo = _pos.ColNo, LineNo = _pos.LineNo };
            }

            set
            {
                _pos.CharIndex = value.CharIndex;
                _pos.ColNo = value.ColNo;
                _pos.LineNo = value.LineNo;
            }
        }

        public bool Eof()
        {
            return !(_pos.CharIndex < _chars.Length);
        }

        public string Substr(TextPosition start, TextPosition end)
        {
            return new string(_chars, start.CharIndex, end.CharIndex - start.CharIndex);
        }

        public bool AnyChar(out char charRead)
        {
            if (_pos.CharIndex < _chars.Length)
            {
                charRead = _chars[_pos.CharIndex++];
                switch ((int)charRead)
                {
                    case 13:
                        _pos._lineStart = _pos.CharIndex;
                        ++_pos.LineNo;
                        _pos.ColNo = 0;
                        break;
                    case 10:
                        _pos._lineStart = _pos.CharIndex;
                        break;
                    default:
                        ++_pos.ColNo;
                        break;
                }
                return true;
            }
            charRead = '\x00';
            return false;
        }

        public bool ThisCharNoSkip(string wantedCharSet, out char charRead)
        {
            var priorPos = Position;
            if (AnyChar(out charRead))
            {
                if (wantedCharSet.IndexOf(charRead.ToString()) != -1)
                    return true;
                Position = priorPos;
            }
            return false;
        }

        public bool ThisCharNoSkip(string wantedCharSet)
        {
            char foo;
            return ThisCharNoSkip(wantedCharSet, out foo);
        }

        public bool ThisCharNoSkip(char wantedChar)
        {
            return ThisCharNoSkip(wantedChar.ToString());
        }

        public bool AnyQuoted(out string textRead)
        {
            var prior = Position;
            Skip();
            char endCh = '\x00';
            if (ThisCharNoSkip("'\"", out endCh))
            {
                var start = Position;
                char ch;
                while (AnyChar(out ch))
                    if (ch == endCh)
                    {
                        textRead = Substr(start, Position);
                        textRead = textRead.Substring(0, textRead.Length - 1);
                        return true;
                    }
            }
            Position = prior;
            textRead = null;
            return false;
        }

        public bool ThisQuoted(string text)
        {
            var prior = Position;
            string textRead = null;
            if (AnyQuoted(out textRead))
                if (textRead == text)
                    return true;
            Position = prior;
            return false;
        }

        public bool Spaces()
        {
            int skipped = 0;
            while (ThisCharNoSkip(SpaceCharSet))
                ++skipped;
            return skipped > 0;
        }

        public bool EndOfLine()
        {
            var ocurrs = 0;
            if (ThisCharNoSkip('\x0d'))
                ++ocurrs;
            if (ThisCharNoSkip('\x0a'))
                ++ocurrs;
            if (ThisCharNoSkip('\x1a'))
                ++ocurrs;
            return ocurrs > 0 || Eof();
        }

        public virtual bool Comments()
        {
            return false;
        }

        public bool Skip()
        {
            int loops = 0;
            while (Spaces() || Comments())
                ++loops;
            return loops > 0;
        }

        public bool ThisSetNoSkip(string wantedCharSet, out string strRead)
        {
            var tempStr = new StringBuilder();
            char charRead = '\x00';
            while (ThisCharNoSkip(wantedCharSet, out charRead))
                tempStr.Append(charRead);
            if (tempStr.Length > 0)
            {
                strRead = tempStr.ToString();
                return true;
            }
            strRead = null;
            return false;
        }

        public string LastWordRead = "";

        public bool AnyWord(out string wordRead)
        {
            Skip();
            char charRead;
            if (ThisCharNoSkip(AlphaCharSet, out charRead))
            {
                var tempStr = new StringBuilder();
                tempStr.Append(charRead);
                while (ThisCharNoSkip(AlphaCharSetEx, out charRead))
                    tempStr.Append(charRead);
                wordRead = tempStr.ToString();
                LastWordRead = wordRead;
                return true;
            }
            wordRead = null;
            return false;
        }

        public bool ThisWord(string wantedWord, out string wordRead)
        {
            var priorPos = Position;
            if (AnyWord(out wordRead))
            {
                if (wordRead == wantedWord)
                    return true;
                Position = priorPos;
            }
            return false;
        }

        public bool ThisWord(string wantedWord)
        {
            string foo;
            return ThisWord(wantedWord, out foo);
        }

        public bool ThisWord(string[] wantedWordSet, out int readIndex)
        {
            var priorPos = Position;
            string wordRead = null;
            if (AnyWord(out wordRead))
            {
                for (int i = 0; i < wantedWordSet.Length; i++)
                    if (wantedWordSet[i] == wordRead)
                    {
                        readIndex = i;
                        return true;
                    }
                Position = priorPos;
            }
            readIndex = -1;
            return false;
        }

        public bool ThisWord(string[] wantedWordSet)
        {
            int foo;
            return ThisWord(wantedWordSet, out foo);
        }

        public bool ThisText(string wantedText)
        {
            Skip();
            var priorPos = Position;
            var wantedChars = wantedText.ToCharArray();
            for (int i = 0; i < wantedChars.Length; i++)
                if (!ThisCharNoSkip(wantedChars[i]))
                {
                    Position = priorPos;
                    return false;
                }
            return true;
        }

        public bool ThisTextNoSkip(string wantedSeq)
        {
            var priorPos = Position;
            var wantedChars = wantedSeq.ToCharArray();
            for (int i = 0; i < wantedChars.Length; i++)
                if (!ThisCharNoSkip(wantedChars[i]))
                {
                    Position = priorPos;
                    return false;
                }
            return true;
        }

        public bool ThisText(string[] wantedTextSet, out string textRead)
        {
            foreach (var text in wantedTextSet)
                if (ThisText(text))
                {
                    textRead = text;
                    return true;
                }
            textRead = null;
            return false;
        }

        public bool ThisText(string[] wantedTextSet, out int textIndex)
        {
            for (int i = 0; i < wantedTextSet.Length; i++)
                if (ThisText(wantedTextSet[i]))
                {
                    textIndex = i;
                    return true;
                }
            textIndex = -1;
            return false;
        }
    }
}
