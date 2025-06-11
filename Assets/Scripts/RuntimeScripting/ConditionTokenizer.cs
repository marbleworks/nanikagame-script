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
            if (IsAtEnd)
            {
                return new ConditionToken(ConditionTokenType.Eof, string.Empty);
            }

            var c = Current;
            switch (c)
            {
                case '(': Advance(); return new ConditionToken(ConditionTokenType.LParen, "(");
                case ')': Advance(); return new ConditionToken(ConditionTokenType.RParen, ")");
                case ',': Advance(); return new ConditionToken(ConditionTokenType.Comma, ",");
                case '!': Advance(); return new ConditionToken(ConditionTokenType.Not, "!");
                case '+': Advance(); return new ConditionToken(ConditionTokenType.Plus, "+");
                case '-': Advance(); return new ConditionToken(ConditionTokenType.Minus, "-");
                case '*': Advance(); return new ConditionToken(ConditionTokenType.Star, "*");
                case '/': Advance(); return new ConditionToken(ConditionTokenType.Slash, "/");
                case '"':
                    var str = ReadStringLiteral();
                    return new ConditionToken(ConditionTokenType.String, str);
            }

            if (c == '&' && Peek(1) == '&')
            {
                Advance();
                Advance();
                return new ConditionToken(ConditionTokenType.And, "&&");
            }

            if (c == '|' && Peek(1) == '|')
            {
                Advance();
                Advance();
                return new ConditionToken(ConditionTokenType.Or, "||");
            }

            if (c == '<')
            {
                if (Peek(1) == '=')
                {
                    Advance();
                    Advance();
                    return new ConditionToken(ConditionTokenType.LessEqual, "<=");
                }
                Advance();
                return new ConditionToken(ConditionTokenType.Less, "<");
            }

            if (c == '>')
            {
                if (Peek(1) == '=')
                {
                    Advance();
                    Advance();
                    return new ConditionToken(ConditionTokenType.GreaterEqual, ">=");
                }
                Advance();
                return new ConditionToken(ConditionTokenType.Greater, ">");
            }

            if (c == '=' && Peek(1) == '=')
            {
                Advance();
                Advance();
                return new ConditionToken(ConditionTokenType.Equal, "==");
            }

            if (char.IsDigit(c) || (c == '.' && PeekDigit()))
            {
                var number = ReadNumberLiteral();
                return new ConditionToken(ConditionTokenType.Number, number);
            }

            if (IsIdentifierStart(c))
            {
                var ident = ReadIdentifierLiteral();
                return new ConditionToken(ConditionTokenType.Identifier, ident);
            }

            throw new InvalidOperationException($"Invalid character '{c}' at position {Index}");
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
