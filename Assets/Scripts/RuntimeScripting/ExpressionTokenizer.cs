using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for arithmetic expressions.
    /// </summary>
    internal sealed class ExpressionTokenizer : TokenizerBase
    {
        public ExpressionTokenizer(string text) : base(text)
        {
        }

        public ExprToken Next()
        {
            SkipWhitespace();

            if (_index >= _text.Length)
            {
                return new ExprToken(ExprTokenType.Eof, string.Empty);
            }

            var ch = _text[_index];
            return ch switch
            {
                '+' => ReturnToken(ExprTokenType.Plus, "+"),
                '-' => ReturnToken(ExprTokenType.Minus, "-"),
                '*' => ReturnToken(ExprTokenType.Star, "*"),
                '/' => ReturnToken(ExprTokenType.Slash, "/"),
                '(' => ReturnToken(ExprTokenType.LParen, "("),
                ')' => ReturnToken(ExprTokenType.RParen, ")"),
                ',' => ReturnToken(ExprTokenType.Comma, ","),
                '"' => ReadString(),
                _ when char.IsDigit(ch) || (ch == '.' && PeekDigit()) => ReadNumber(),
                _ when char.IsLetter(ch) || IsIdentifierStart(ch) => ReadIdentifier(),
                _ => throw new InvalidOperationException($"Invalid character at position {_index}: '{ch}'")
            };
        }

        private ExprToken ReturnToken(ExprTokenType type, string value)
        {
            _index++;
            return new ExprToken(type, value);
        }

        private ExprToken ReadString()
        {
            var content = ReadStringLiteral();
            return new ExprToken(ExprTokenType.String, content);
        }

        private ExprToken ReadNumber()
        {
            var number = ReadNumberLiteral();
            return new ExprToken(ExprTokenType.Number, number);
        }

        private bool PeekDigit() => base.PeekDigit();

        private ExprToken ReadIdentifier()
        {
            var start = _index;
            while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || IsIdentifierPart(_text[_index])))
            {
                _index++;
            }

            return new ExprToken(ExprTokenType.Identifier, _text[start.._index]);
        }        }
    }

    internal enum ExprTokenType
    {
        Eof,
        Number,
        Identifier,
        String,
        Plus,
        Minus,
        Star,
        Slash,
        LParen,
        RParen,
        Comma
    }

    internal readonly struct ExprToken
    {
        public ExprTokenType Type { get; }
        public string Value { get; }

        public ExprToken(ExprTokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
