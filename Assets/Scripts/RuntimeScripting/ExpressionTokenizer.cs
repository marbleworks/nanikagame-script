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

            if (Index >= Text.Length)
            {
                return new ExprToken(ExprTokenType.Eof, string.Empty);
            }

            var ch = Text[Index];
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
                _ => throw new InvalidOperationException($"Invalid character at position {Index}: '{ch}'")
            };
        }

        private ExprToken ReturnToken(ExprTokenType type, string value)
        {
            Index++;
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

        private ExprToken ReadIdentifier()
        {
            var start = Index;
            while (Index < Text.Length && (char.IsLetterOrDigit(Text[Index]) || IsIdentifierPart(Text[Index])))
            {
                Index++;
            }

            return new ExprToken(ExprTokenType.Identifier, Text[start..Index]);
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
