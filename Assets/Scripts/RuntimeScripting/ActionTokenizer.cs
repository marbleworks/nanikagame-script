using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Tokenizer for action lists and modifier blocks.
    /// Provides tokens for identifiers, numbers, strings and punctuation.
    /// </summary>
    internal sealed class ActionTokenizer
    {
        private readonly string _text;
        private int _index;

        public ActionTokenizer(string text)
        {
            _text = text ?? string.Empty;
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
            var quote = _text[_index];
            _index++;
            var start = _index;
            while (_index < _text.Length && _text[_index] != quote)
            {
                if (_text[_index] == '\\' && _index + 1 < _text.Length)
                    _index += 2;
                else
                    _index++;
            }

            var str = _text.Substring(start, _index - start);
            if (_index < _text.Length && _text[_index] == quote)
                _index++;
            return new ActionToken(ActionTokenType.String, str);
        }

        private ActionToken ReadNumber()
        {
            var start = _index;
            var hasDot = false;
            if (_text[_index] == '.')
            {
                hasDot = true;
                _index++;
            }

            while (_index < _text.Length)
            {
                var ch = _text[_index];
                if (char.IsDigit(ch))
                {
                    _index++;
                }
                else if (ch == '.' && !hasDot)
                {
                    hasDot = true;
                    _index++;
                }
                else
                {
                    break;
                }
            }
            return new ActionToken(ActionTokenType.Number, _text.Substring(start, _index - start));
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

        private static bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '@' || ch == '#' || ch == '_' || ch == '[' || ch == ']';
        private static bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || char.IsDigit(ch);
        private bool PeekDigit() => _index + 1 < _text.Length && char.IsDigit(_text[_index + 1]);

        private void SkipWhitespace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                _index++;
        }
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
