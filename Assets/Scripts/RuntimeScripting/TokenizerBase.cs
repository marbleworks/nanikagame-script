using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Provides common tokenizer utilities for derived tokenizers.
    /// </summary>
    internal abstract class TokenizerBase
    {
        protected readonly string _text;
        protected int _index;

        protected TokenizerBase(string text)
        {
            _text = text ?? string.Empty;
        }

        /// <summary>
        /// Skips whitespace characters.
        /// </summary>
        protected void SkipWhitespace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }

        /// <summary>
        /// Reads a quoted string and returns the unescaped content.
        /// </summary>
        protected string ReadStringLiteral()
        {
            var quote = _text[_index];
            _index++;
            var start = _index;
            while (_index < _text.Length && _text[_index] != quote)
            {
                if (_text[_index] == '\\' && _index + 1 < _text.Length)
                {
                    _index += 2;
                }
                else
                {
                    _index++;
                }
            }

            var str = _text.Substring(start, _index - start);
            if (_index < _text.Length && _text[_index] == quote)
                _index++;
            return str;
        }

        /// <summary>
        /// Reads a numeric literal as a string.
        /// </summary>
        protected string ReadNumberLiteral()
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

            return _text.Substring(start, _index - start);
        }

        /// <summary>
        /// Returns the character at the specified offset from the current index
        /// or '\0' if out of range.
        /// </summary>
        protected char Peek(int offset)
        {
            var pos = _index + offset;
            return pos < _text.Length ? _text[pos] : '\0';
        }

        /// <summary>
        /// Returns true if the next character is a digit.
        /// </summary>
        protected bool PeekDigit() => _index + 1 < _text.Length && char.IsDigit(_text[_index + 1]);

        /// <summary>
        /// Determines if the character can begin an identifier.
        /// </summary>
        protected virtual bool IsIdentifierStart(char ch)
            => char.IsLetter(ch) || ch == '@' || ch == '#' || ch == '_' || ch == '[' || ch == ']' || ch == '=';

        /// <summary>
        /// Determines if the character can be part of an identifier.
        /// </summary>
        protected virtual bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || char.IsDigit(ch);
    }
}

