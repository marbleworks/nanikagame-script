using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for action lists and modifier blocks.
    /// Provides tokens for identifiers, numbers, strings and punctuation.
    /// </summary>
    internal sealed class ActionTokenizer : TokenizerBase
    {
        public ActionTokenizer(string text) : base(text)
        {
        }

        public ActionToken Next()
        {
            SkipWhitespace();
            if (_index >= _text.Length)
            {
                return new ActionToken(ActionTokenType.Eof, string.Empty);
            }

            var c = _text[_index];
            switch (c)
            {
                case '(': _index++; return new ActionToken(ActionTokenType.LParen, "(");
                case ')': _index++; return new ActionToken(ActionTokenType.RParen, ")");
                case ',': _index++; return new ActionToken(ActionTokenType.Comma, ",");
                case '=': _index++; return new ActionToken(ActionTokenType.Assign, "=");
                case '"':
                case '\'':
                    return ReadString();
            }

            if (char.IsDigit(c) || (c == '.' && PeekDigit()))
            {
                return ReadNumber();
            }

            if (IsIdentifierStart(c))
            {
                return ReadIdentifier();
            }

            throw new InvalidOperationException($"Invalid character '{c}' at {_index}");
        }

        private ActionToken ReadString()
        {
            var str = ReadStringLiteral();
            return new ActionToken(ActionTokenType.String, str);
        }

        private ActionToken ReadNumber()
        {
            var number = ReadNumberLiteral();
            return new ActionToken(ActionTokenType.Number, number);
        }

        private ActionToken ReadIdentifier()
        {
            var start = _index;
            _index++;
            while (_index < _text.Length && IsIdentifierPart(_text[_index]))
            {
                _index++;
            }
            return new ActionToken(ActionTokenType.Identifier, _text.Substring(start, _index - start));
        }

        private bool PeekDigit() => base.PeekDigit();
    }

    internal enum ActionTokenType
    {
        Eof,
        Identifier,
        Number,
        String,
        LParen,
        RParen,
        Comma,
        Assign
    }

    internal readonly struct ActionToken
    {
        public ActionTokenType Type { get; }
        public string Value { get; }

        public ActionToken(ActionTokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
