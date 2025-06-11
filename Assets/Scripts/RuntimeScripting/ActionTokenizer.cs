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
            if (Index >= Text.Length)
            {
                return new ActionToken(ActionTokenType.Eof, string.Empty);
            }

            var c = Text[Index];
            switch (c)
            {
                case '(': Index++; return new ActionToken(ActionTokenType.LParen, "(");
                case ')': Index++; return new ActionToken(ActionTokenType.RParen, ")");
                case ',': Index++; return new ActionToken(ActionTokenType.Comma, ",");
                case '=': Index++; return new ActionToken(ActionTokenType.Assign, "=");
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
            var start = Index;
            Index++;
            while (Index < Text.Length && IsIdentifierPart(Text[Index]))
            {
                Index++;
            }
            return new ActionToken(ActionTokenType.Identifier, Text.Substring(start, Index - start));
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
