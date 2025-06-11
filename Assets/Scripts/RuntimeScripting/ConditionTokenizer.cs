using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for boolean condition expressions.
    /// </summary>
    internal sealed class ConditionTokenizer : TokenizerBase
    {
        public ConditionTokenizer(string text) : base(text)
        {
        }

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
                    var str = ReadStringLiteral();
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

            if (char.IsDigit(c) || (c == '.' && PeekDigit()))
            {
                var number = ReadNumberLiteral();
                return new ConditionToken(ConditionTokenType.Number, number);
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

        private bool PeekDigit() => base.PeekDigit();
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
