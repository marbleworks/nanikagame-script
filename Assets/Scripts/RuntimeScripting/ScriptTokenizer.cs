using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Simple helper for scanning script text character by character.
    /// Provides utilities used by the DSL parser.
    /// </summary>
    internal sealed class ScriptTokenizer : TokenizerBase
    {
        private ScriptToken? _peeked;

        public ScriptTokenizer(string text) : base(text ?? throw new ArgumentNullException(nameof(text)))
        {
        }

        /// <summary>
        /// Returns the next token in the stream.
        /// </summary>
        public ScriptToken NextToken()
        {
            while (true)
            {
                if (_peeked.HasValue)
                {
                    var t = _peeked.Value;
                    _peeked = null;
                    return t;
                }

                SkipWhite();
                if (IsAtEnd) return new ScriptToken(ScriptTokenType.Eof, string.Empty);

                var c = Current;

                switch (c)
                {
                    case '[':
                        Advance();
                        return new ScriptToken(ScriptTokenType.LBracket, "[");
                    case ']':
                        Advance();
                        return new ScriptToken(ScriptTokenType.RBracket, "]");
                    case '(':
                        Advance();
                        return new ScriptToken(ScriptTokenType.LParen, "(");
                    case ')':
                        Advance();
                        return new ScriptToken(ScriptTokenType.RParen, ")");
                    case '{':
                        Advance();
                        return new ScriptToken(ScriptTokenType.LBrace, "{");
                    case '}':
                        Advance();
                        return new ScriptToken(ScriptTokenType.RBrace, "}");
                    case ',':
                        Advance();
                        return new ScriptToken(ScriptTokenType.Comma, ",");
                    case ';':
                        Advance();
                        return new ScriptToken(ScriptTokenType.Semicolon, ";");
                    case '=':
                        Advance();
                        return new ScriptToken(ScriptTokenType.Assign, "=");
                    case '#':
                        SkipLine();
                        continue;
                    case '"':
                    case '\'':
                        return ReadString();
                }

                if (char.IsDigit(c) || (c == '.' && PeekDigit())) return ReadNumber();

                if (char.IsLetter(c) || IsIdentifierStart(c)) return ReadIdentifier();

                throw new InvalidOperationException($"Invalid character '{c}' at position {Index}");
            }
        }

        /// <summary>
        /// Peeks the next token without consuming it.
        /// </summary>
        public ScriptToken PeekToken()
        {
            _peeked ??= NextToken();
            return _peeked.Value;
        }

        /// <summary>
        /// Consumes the next token and ensures it matches the expected type.
        /// </summary>
        public void Expect(ScriptTokenType type)
        {
            var token = NextToken();
            if (token.Type != type)
                throw new Exception($"Expected token {type} at {Index}");
        }

        /// <summary>
        /// Gets the current character or null character if at end of text.
        /// </summary>
        public char Peek() => Current;

        /// <summary>
        /// Skips whitespace characters and returns true if not at end.
        /// </summary>
        public bool SkipWhite()
        {
            SkipWhitespace();
            return !IsAtEnd;
        }

        /// <summary>
        /// Skips characters until the next newline.
        /// </summary>
        public void SkipLine()
        {
            while (Index < Text.Length && Text[Index] != '\n')
                Index++;
            if (Index < Text.Length)
                Index++;
            _peeked = null;
        }

        /// <summary>
        /// Returns true if the upcoming characters match the provided string.
        /// </summary>
        public bool StartsWith(string s)
        {
            SkipWhite();
            return string.Compare(Text, Index, s, 0, s.Length, StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Reads characters until the specified character is encountered.
        /// </summary>
        public string ReadUntil(char c)
        {
            var start = Index;
            while (Index < Text.Length && Text[Index] != c)
                Index++;
            return Text.Substring(start, Index - start);
        }

        /// <summary>
        /// Reads a substring enclosed by matching open/close characters without consuming the closing character.
        /// Handles nested pairs and quoted strings.
        /// </summary>
        public string ReadEnclosed(char open, char close)
        {
            var depth = 1;
            var start = Index;
            var inString = false;
            var stringChar = '\0';

            while (Index < Text.Length)
            {
                var ch = Text[Index];

                if (inString)
                {
                    if (ch == '\\' && Index + 1 < Text.Length)
                    {
                        // Skip escaped characters inside strings
                        Index += 2;
                        continue;
                    }

                    if (ch == stringChar)
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (ch is '"' or '\'')
                    {
                        inString = true;
                        stringChar = ch;
                    }
                    else if (ch == open)
                    {
                        depth++;
                    }
                    else if (ch == close)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return Text.Substring(start, Index - start);
                        }
                    }
                }

                Index++;
            }

            return Text[start..];
        }

        /// <summary>
        /// Ensures the next character matches the expected value.
        /// </summary>
        public void Expect(char c)
        {
            SkipWhite();
            if (IsAtEnd || Current != c)
                throw new Exception($"Expected '{c}' at {Index}");
            Advance();
        }

        /// <summary>
        /// Ensures the upcoming characters match the provided string.
        /// </summary>
        public void ExpectString(string s)
        {
            SkipWhite();
            for (var i = 0; i < s.Length; i++)
            {
                if (Index + i >= Text.Length || Text[Index + i] != s[i])
                    throw new Exception($"Expected '{s}' at {Index}");
            }

            Index += s.Length;
        }

        /// <summary>
        /// Returns a substring with surrounding quotes removed if present.
        /// </summary>
        public static string Unquote(string value)
        {
            if (value.Length >= 2)
            {
                if ((value[0] == '"' && value[^1] == '"') ||
                    (value[0] == '\'' && value[^1] == '\''))
                {
                    return value.Substring(1, value.Length - 2);
                }
            }

            return value;
        }

        private ScriptToken ReadString()
        {
            var str = ReadStringLiteral();
            return new ScriptToken(ScriptTokenType.String, str);
        }

        private ScriptToken ReadNumber()
        {
            var number = ReadNumberLiteral();
            return new ScriptToken(ScriptTokenType.Number, number);
        }

        private ScriptToken ReadIdentifier()
        {
            var ident = ReadIdentifierLiteral();
            return ident switch
            {
                "if" => new ScriptToken(ScriptTokenType.If, ident),
                "else" => new ScriptToken(ScriptTokenType.Else, ident),
                "act" => new ScriptToken(ScriptTokenType.Act, ident),
                "mod" => new ScriptToken(ScriptTokenType.Mod, ident),
                _ => new ScriptToken(ScriptTokenType.Identifier, ident)
            };
        }
    }

    internal enum ScriptTokenType
    {
        Eof,
        Identifier,
        Number,
        String,
        LBracket,
        RBracket,
        LParen,
        RParen,
        LBrace,
        RBrace,
        Comma,
        Semicolon,
        Assign,
        If,
        Else,
        Act,
        Mod
    }

    internal readonly struct ScriptToken
    {
        public ScriptTokenType Type { get; }
        public string Value { get; }

        public ScriptToken(ScriptTokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
