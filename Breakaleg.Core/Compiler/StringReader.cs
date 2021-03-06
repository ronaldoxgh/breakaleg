using System;
using System.Linq;
using System.Text;

namespace Breakaleg.Core.Compiler
{
    ///TODO case-sensitiveness
    public class StringReader
    {
        public const string AlphaCharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";
        public const string DigitCharSet = "0123456789";
        public const string AlphaCharSetEx = AlphaCharSet + DigitCharSet;
        public const string SpaceCharSet = "\x0d\x0a\x20\x09\x1a";
        public const string HexCharSet = "0123456789abcdefABCDEF";

        private char[] _charBuffer;
        private TextPosition _currentPosition = new TextPosition();

        public StringReader(string sourceCode)
        {
            _charBuffer = sourceCode.ToCharArray();
            _currentPosition = new TextPosition
            {
                CharIndex = 0,
                LineNo = 1,
                ColNo = 1
            };
        }

        public string CurrentLine
        {
            get
            {
                return new string(_charBuffer, _currentPosition.LineStart, _currentPosition.CharIndex - _currentPosition.LineStart);
            }
        }

        public string RestOf
        {
            get
            {
                return new string(_charBuffer, _currentPosition.LineStart, 80);
            }
        }

        public TextPosition Position
        {
            get
            {
                return new TextPosition
                {
                    CharIndex = _currentPosition.CharIndex,
                    ColNo = _currentPosition.ColNo,
                    LineNo = _currentPosition.LineNo
                };
            }

            set
            {
                _currentPosition.CharIndex = value.CharIndex;
                _currentPosition.ColNo = value.ColNo;
                _currentPosition.LineNo = value.LineNo;
            }
        }

        public bool Eof()
        {
            return !(_currentPosition.CharIndex < _charBuffer.Length);
        }

        public string Substr(TextPosition start, TextPosition end)
        {
            return new string(_charBuffer, start.CharIndex, end.CharIndex - start.CharIndex);
        }

        public bool AnyChar(out char charRead)
        {
            if (_currentPosition.CharIndex < _charBuffer.Length)
            {
                charRead = _charBuffer[_currentPosition.CharIndex++];
                switch ((int)charRead)
                {
                    case 13:
                        _currentPosition.LineStart = _currentPosition.CharIndex;
                        ++_currentPosition.LineNo;
                        _currentPosition.ColNo = 0;
                        break;
                    case 10:
                        _currentPosition.LineStart = _currentPosition.CharIndex;
                        break;
                    default:
                        ++_currentPosition.ColNo;
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
            var sb = new StringBuilder();
            char endCh = '\x00';
            if (ThisCharNoSkip("'\"", out endCh))
            {
                char ch;
                while (AnyChar(out ch))
                    if (ch == '\\')
                    {
                        if (!AnyChar(out ch))
                            throw new Exception("unterminated string literal");
                        if (ch == 'n')
                            sb.Append('\n');
                        else if (ch == '\r')
                        {
                            sb.Append(ch);
                            ThisCharNoSkip('\n');
                        }
                        else
                            sb.Append(ch);
                    }
                    else if (ch == endCh)
                    {
                        textRead = sb.ToString();
                        return true;
                    }
                    else
                        sb.Append(ch);
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
            var skipped = 0;
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
            var loops = 0;
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
                for (var i = 0; i < wantedWordSet.Length; i++)
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
            if (wantedChars.Any(t => !ThisCharNoSkip(t)))
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
            if (wantedChars.Any(t => !ThisCharNoSkip(t)))
            {
                Position = priorPos;
                return false;
            }
            return true;
        }

        public bool ThisText(string[] wantedTextSet, out string textRead)
        {
            foreach (var text in wantedTextSet.Where(ThisText))
            {
                textRead = text;
                return true;
            }
            textRead = null;
            return false;
        }

        public bool ThisText(string[] wantedTextSet, out int textIndex)
        {
            for (var i = 0; i < wantedTextSet.Length; i++)
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