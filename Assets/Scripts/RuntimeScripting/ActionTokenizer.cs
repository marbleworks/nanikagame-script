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
            if (IsAtEnd)
            {
                return new ActionToken(ActionTokenType.Eof, string.Empty);
            }

            var c = Current;
            switch (c)
            {
                case '(': Advance(); return new ActionToken(ActionTokenType.LParen, "(");
                case ')': Advance(); return new ActionToken(ActionTokenType.RParen, ")");
                case ',': Advance(); return new ActionToken(ActionTokenType.Comma, ",");
                case '=': Advance(); return new ActionToken(ActionTokenType.Assign, "=");
                case '+': Advance(); return new ActionToken(ActionTokenType.Plus, "+");
                case '-': Advance(); return new ActionToken(ActionTokenType.Minus, "-");
                case '*': Advance(); return new ActionToken(ActionTokenType.Star, "*");
                case '/': Advance(); return new ActionToken(ActionTokenType.Slash, "/");
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

            throw new InvalidOperationException($"Invalid character '{c}' at {Index}");
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
            var ident = ReadIdentifierLiteral();
            return new ActionToken(ActionTokenType.Identifier, ident);
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
        Assign,
        Plus,
        Minus,
        Star,
        Slash
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
