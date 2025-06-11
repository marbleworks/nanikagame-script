using System;
using System.Text;

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
            if (_peeked.HasValue)
            {
                var t = _peeked.Value;
                _peeked = null;
                return t;
            }

            SkipWhite();
            if (_index >= _text.Length)
                return new ScriptToken(ScriptTokenType.Eof, string.Empty);

            var c = _text[_index];

            switch (c)
            {
                case '[': _index++; return new ScriptToken(ScriptTokenType.LBracket, "[");
                case ']': _index++; return new ScriptToken(ScriptTokenType.RBracket, "]");
                case '(': _index++; return new ScriptToken(ScriptTokenType.LParen, "(");
                case ')': _index++; return new ScriptToken(ScriptTokenType.RParen, ")");
                case '{': _index++; return new ScriptToken(ScriptTokenType.LBrace, "{");
                case '}': _index++; return new ScriptToken(ScriptTokenType.RBrace, "}");
                case ',': _index++; return new ScriptToken(ScriptTokenType.Comma, ",");
                case ';': _index++; return new ScriptToken(ScriptTokenType.Semicolon, ";");
                case '=': _index++; return new ScriptToken(ScriptTokenType.Assign, "=");
                case '#':
                    SkipLine();
                    return NextToken();
                case '"':
                case '\'':
                    return ReadString();
            }

            if (char.IsDigit(c) || (c == '.' && _index + 1 < _text.Length && char.IsDigit(_text[_index + 1])))
                return ReadNumber();

            if (char.IsLetter(c) || c == '_' || c == '@' || c == '#')
                return ReadIdentifier();

            throw new InvalidOperationException($"Invalid character '{c}' at position {_index}");
        }

        /// <summary>
        /// Peeks the next token without consuming it.
        /// </summary>
        public ScriptToken PeekToken()
        {
            if (!_peeked.HasValue)
                _peeked = NextToken();
            return _peeked.Value;
        }

        /// <summary>
        /// Consumes the next token and ensures it matches the expected type.
        /// </summary>
        public void Expect(ScriptTokenType type)
        {
            var token = NextToken();
            if (token.Type != type)
                throw new Exception($"Expected token {type} at {_index}");
        }

        /// <summary>
        /// Gets the current character or null character if at end of text.
        /// </summary>
        public char Peek() => _index >= _text.Length ? '\0' : _text[_index];

        /// <summary>
        /// Skips whitespace characters and returns true if not at end.
        /// </summary>
        public bool SkipWhite()
        {
            SkipWhitespace();
            return _index < _text.Length;
        }

        /// <summary>
        /// Skips characters until the next newline.
        /// </summary>
        public void SkipLine()
        {
            while (_index < _text.Length && _text[_index] != '\n')
                _index++;
            if (_index < _text.Length)
                _index++;
            _peeked = null;
        }

        /// <summary>
        /// Returns true if the upcoming characters match the provided string.
        /// </summary>
        public bool StartsWith(string s)
        {
            SkipWhite();
            return string.Compare(_text, _index, s, 0, s.Length, StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Reads characters until the specified character is encountered.
        /// </summary>
        public string ReadUntil(char c)
        {
            var start = _index;
            while (_index < _text.Length && _text[_index] != c)
                _index++;
            return _text.Substring(start, _index - start);
        }

        /// <summary>
        /// Reads a substring enclosed by matching open/close characters without consuming the closing character.
        /// Handles nested pairs and quoted strings.
        /// </summary>
        public string ReadEnclosed(char open, char close)
        {
            var depth = 1;
            var start = _index;
            var inString = false;
            var stringChar = '\0';

            while (_index < _text.Length)
            {
                var ch = _text[_index];

                if (inString)
                {
                    if (ch == '\\' && _index + 1 < _text.Length)
                    {
                        // Skip escaped characters inside strings
                        _index += 2;
                        continue;
                    }

                    if (ch == stringChar)
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (ch == '"' || ch == '\'')
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
                            return _text.Substring(start, _index - start);
                        }
                    }
                }

                _index++;
            }

            return _text[start..];
        }

        /// <summary>
        /// Ensures the next character matches the expected value.
        /// </summary>
        public void Expect(char c)
        {
            SkipWhite();
            if (_index >= _text.Length || _text[_index] != c)
                throw new Exception($"Expected '{c}' at {_index}");
            _index++;
        }

        /// <summary>
        /// Ensures the upcoming characters match the provided string.
        /// </summary>
        public void ExpectString(string s)
        {
            SkipWhite();
            for (var i = 0; i < s.Length; i++)
            {
                if (_index + i >= _text.Length || _text[_index + i] != s[i])
                    throw new Exception($"Expected '{s}' at {_index}");
            }

            _index += s.Length;
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
            var start = _index;
            while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_' || _text[_index] == '@' || _text[_index] == '#'))
            {
                _index++;
            }

            var ident = _text.Substring(start, _index - start);
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
