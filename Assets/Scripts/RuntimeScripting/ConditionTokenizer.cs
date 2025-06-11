using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for boolean condition expressions.
    /// </summary>
    internal sealed class ConditionTokenizer
    {
        private readonly string _text;
        private int _index;

        public ConditionTokenizer(string text) => _text = text;

        public ConditionToken Next()
        {
            SkipWhitespace();
            if (_index >= _text.Length)
            {
                return new ConditionToken(ConditionTokenType.Eof, string.Empty);
            }

            var c = _text[_index];
            switch (c)
            {
                case '(': _index++; return new ConditionToken(ConditionTokenType.LParen, "(");
                case ')': _index++; return new ConditionToken(ConditionTokenType.RParen, ")");
                case ',': _index++; return new ConditionToken(ConditionTokenType.Comma, ",");
                case '!': _index++; return new ConditionToken(ConditionTokenType.Not, "!");
                case '+': _index++; return new ConditionToken(ConditionTokenType.Plus, "+");
                case '-': _index++; return new ConditionToken(ConditionTokenType.Minus, "-");
                case '*': _index++; return new ConditionToken(ConditionTokenType.Star, "*");
                case '/': _index++; return new ConditionToken(ConditionTokenType.Slash, "/");
                case '"':
                    _index++;
                    var start = _index;
                    while (_index < _text.Length && _text[_index] != '"')
                    {
                        if (_text[_index] == '\\' && _index + 1 < _text.Length)
                        {
                            _index += 2;
                        }
                        else
                        {
                            _index++;
                        }
                    }
                    var str = _text.Substring(start, _index - start);
                    if (_index < _text.Length && _text[_index] == '"')
                    {
                        _index++;
                    }
                    return new ConditionToken(ConditionTokenType.String, str);
            }

            if (c == '&' && Peek(1) == '&')
            {
                _index += 2;
                return new ConditionToken(ConditionTokenType.And, "&&");
            }

            if (c == '|' && Peek(1) == '|')
            {
                _index += 2;
                return new ConditionToken(ConditionTokenType.Or, "||");
            }

            if (c == '<')
            {
                if (Peek(1) == '=')
                {
                    _index += 2;
                    return new ConditionToken(ConditionTokenType.LessEqual, "<=");
                }
                _index++;
                return new ConditionToken(ConditionTokenType.Less, "<");
            }

            if (c == '>')
            {
                if (Peek(1) == '=')
                {
                    _index += 2;
                    return new ConditionToken(ConditionTokenType.GreaterEqual, ">=");
                }
                _index++;
                return new ConditionToken(ConditionTokenType.Greater, ">");
            }

            if (c == '=' && Peek(1) == '=')
            {
                _index += 2;
                return new ConditionToken(ConditionTokenType.Equal, "==");
            }

            if (char.IsDigit(c) || (c == '.' && _index + 1 < _text.Length && char.IsDigit(_text[_index + 1])))
            {
                var start = _index;
                var hasDot = false;
                if (c == '.')
                {
                    hasDot = true;
                    _index++;
                }
                while (_index < _text.Length)
                {
                    var nc = _text[_index];
                    if (char.IsDigit(nc))
                    {
                        _index++;
                    }
                    else if (nc == '.' && !hasDot)
                    {
                        hasDot = true;
                        _index++;
                    }
                    else
                    {
                        break;
                    }
                }
                return new ConditionToken(ConditionTokenType.Number, _text.Substring(start, _index - start));
            }

            if (char.IsLetter(c) || c == '@' || c == '#' || c == '_' || c == '[' || c == ']' || c == '=')
            {
                var start = _index;
                while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || "_@#[]=".IndexOf(_text[_index]) >= 0))
                {
                    _index++;
                }
                return new ConditionToken(ConditionTokenType.Identifier, _text.Substring(start, _index - start));
            }

            throw new InvalidOperationException($"Invalid character '{c}' at position {_index}");
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }

        private char Peek(int offset)
        {
            var pos = _index + offset;
            return pos < _text.Length ? _text[pos] : '\0';
        }
    }

    internal enum ConditionTokenType
    {
        Eof,
        Identifier,
        Number,
        String,
        LParen,
        RParen,
        Comma,
        Plus,
        Minus,
        Star,
        Slash,
        And,
        Or,
        Not,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        Equal
    }

    internal readonly struct ConditionToken
    {
        public ConditionTokenType Type { get; }
        public string Value { get; }

        public ConditionToken(ConditionTokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
