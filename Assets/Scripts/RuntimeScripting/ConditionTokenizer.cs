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
            if (Index >= Text.Length)
            {
                return new ConditionToken(ConditionTokenType.Eof, string.Empty);
            }

            var c = Text[Index];
            switch (c)
            {
                case '(': Index++; return new ConditionToken(ConditionTokenType.LParen, "(");
                case ')': Index++; return new ConditionToken(ConditionTokenType.RParen, ")");
                case ',': Index++; return new ConditionToken(ConditionTokenType.Comma, ",");
                case '!': Index++; return new ConditionToken(ConditionTokenType.Not, "!");
                case '+': Index++; return new ConditionToken(ConditionTokenType.Plus, "+");
                case '-': Index++; return new ConditionToken(ConditionTokenType.Minus, "-");
                case '*': Index++; return new ConditionToken(ConditionTokenType.Star, "*");
                case '/': Index++; return new ConditionToken(ConditionTokenType.Slash, "/");
                case '"':
                    var str = ReadStringLiteral();
                    return new ConditionToken(ConditionTokenType.String, str);
            }

            if (c == '&' && Peek(1) == '&')
            {
                Index += 2;
                return new ConditionToken(ConditionTokenType.And, "&&");
            }

            if (c == '|' && Peek(1) == '|')
            {
                Index += 2;
                return new ConditionToken(ConditionTokenType.Or, "||");
            }

            if (c == '<')
            {
                if (Peek(1) == '=')
                {
                    Index += 2;
                    return new ConditionToken(ConditionTokenType.LessEqual, "<=");
                }
                Index++;
                return new ConditionToken(ConditionTokenType.Less, "<");
            }

            if (c == '>')
            {
                if (Peek(1) == '=')
                {
                    Index += 2;
                    return new ConditionToken(ConditionTokenType.GreaterEqual, ">=");
                }
                Index++;
                return new ConditionToken(ConditionTokenType.Greater, ">");
            }

            if (c == '=' && Peek(1) == '=')
            {
                Index += 2;
                return new ConditionToken(ConditionTokenType.Equal, "==");
            }

            if (char.IsDigit(c) || (c == '.' && PeekDigit()))
            {
                var number = ReadNumberLiteral();
                return new ConditionToken(ConditionTokenType.Number, number);
            }

            if (char.IsLetter(c) || c == '@' || c == '#' || c == '_' || c == '[' || c == ']' || c == '=')
            {
                var start = Index;
                while (Index < Text.Length && (char.IsLetterOrDigit(Text[Index]) || "_@#[]=".IndexOf(Text[Index]) >= 0))
                {
                    Index++;
                }
                return new ConditionToken(ConditionTokenType.Identifier, Text.Substring(start, Index - start));
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
