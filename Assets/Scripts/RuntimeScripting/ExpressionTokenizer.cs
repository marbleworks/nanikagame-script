using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for arithmetic expressions.
    /// </summary>
    internal sealed class ExpressionTokenizer
    {
        private readonly string _text;
        private int _position;

        public ExpressionTokenizer(string text) => (_text, _position) = (text, 0);

        public ExprToken Next()
        {
            SkipWhitespace();

            if (_position >= _text.Length)
            {
                return new ExprToken(ExprTokenType.Eof, string.Empty);
            }

            var ch = _text[_position];
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
                _ => throw new InvalidOperationException($"Invalid character at position {_position}: '{ch}'")
            };
        }

        private ExprToken ReturnToken(ExprTokenType type, string value)
        {
            _position++;
            return new ExprToken(type, value);
        }

        private ExprToken ReadString()
        {
            _position++;
            var start = _position;
            while (_position < _text.Length && _text[_position] != '"')
            {
                if (_text[_position] == '\\' && _position + 1 < _text.Length)
                {
                    _position += 2;
                }
                else
                {
                    _position++;
                }
            }

            var content = _text[start.._position];
            if (_position < _text.Length && _text[_position] == '"')
            {
                _position++;
            }

            return new ExprToken(ExprTokenType.String, content);
        }

        private ExprToken ReadNumber()
        {
            var start = _position;
            var hasDot = false;
            if (_text[_position] == '.')
            {
                hasDot = true;
                _position++;
            }

            while (_position < _text.Length)
            {
                var ch = _text[_position];
                if (char.IsDigit(ch))
                {
                    _position++;
                }
                else if (ch == '.' && !hasDot)
                {
                    hasDot = true;
                    _position++;
                }
                else
                {
                    break;
                }
            }

            return new ExprToken(ExprTokenType.Number, _text[start.._position]);
        }

        private bool PeekDigit() => _position + 1 < _text.Length && char.IsDigit(_text[_position + 1]);

        private ExprToken ReadIdentifier()
        {
            var start = _position;
            while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || IsIdentifierPart(_text[_position])))
            {
                _position++;
            }

            return new ExprToken(ExprTokenType.Identifier, _text[start.._position]);
        }

        private static bool IsIdentifierStart(char ch) => ch is '@' or '#' or '[' or ']' or '=' or '_';

        private static bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || char.IsLetterOrDigit(ch);

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }
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
