using System;
using System.Text;

namespace RuntimeScripting
{
    /// <summary>
    /// Simple helper for scanning script text character by character.
    /// Provides utilities used by the DSL parser.
    /// </summary>
    internal sealed class ScriptTokenizer
    {
        private readonly string _text;
        private int _index;

        public ScriptTokenizer(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
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
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }

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
    }
}
