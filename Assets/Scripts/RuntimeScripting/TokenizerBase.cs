namespace RuntimeScripting
{
    /// <summary>
    /// Provides common tokenizer utilities for derived tokenizers.
    /// </summary>
    internal abstract class TokenizerBase
    {
        protected readonly string Text;
        protected int Index;

        /// <summary>
        /// Gets a value indicating whether the tokenizer has consumed all text.
        /// </summary>
        protected bool IsAtEnd => Index >= Text.Length;

        /// <summary>
        /// Gets the current character or <c>'\0'</c> if at end of text.
        /// </summary>
        protected char Current => IsAtEnd ? '\0' : Text[Index];

        /// <summary>
        /// Advances one character and returns it, or <c>'\0'</c> if at end.
        /// </summary>
        protected char Advance() => IsAtEnd ? '\0' : Text[Index++];

        protected TokenizerBase(string text)
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// Skips whitespace characters.
        /// </summary>
        protected void SkipWhitespace()
        {
            while (Index < Text.Length && char.IsWhiteSpace(Text[Index]))
            {
                Index++;
            }
        }

        /// <summary>
        /// Reads a quoted string and returns the unescaped content.
        /// </summary>
        protected string ReadStringLiteral()
        {
            var quote = Text[Index];
            Index++;
            var start = Index;
            while (Index < Text.Length && Text[Index] != quote)
            {
                if (Text[Index] == '\\' && Index + 1 < Text.Length)
                {
                    Index += 2;
                }
                else
                {
                    Index++;
                }
            }

            var str = Text.Substring(start, Index - start);
            if (Index < Text.Length && Text[Index] == quote)
                Index++;
            return str;
        }

        /// <summary>
        /// Reads a numeric literal as a string.
        /// </summary>
        protected string ReadNumberLiteral()
        {
            var start = Index;
            var hasDot = false;
            if (Text[Index] == '.')
            {
                hasDot = true;
                Index++;
            }

            while (Index < Text.Length)
            {
                var ch = Text[Index];
                if (char.IsDigit(ch))
                {
                    Index++;
                }
                else if (ch == '.' && !hasDot)
                {
                    hasDot = true;
                    Index++;
                }
                else
                {
                    break;
                }
            }

            return Text.Substring(start, Index - start);
        }

        /// <summary>
        /// Returns the character at the specified offset from the current index
        /// or '\0' if out of range.
        /// </summary>
        protected char Peek(int offset)
        {
            var pos = Index + offset;
            return pos < Text.Length ? Text[pos] : '\0';
        }

        /// <summary>
        /// Returns true if the next character is a digit.
        /// </summary>
        protected bool PeekDigit() => Index + 1 < Text.Length && char.IsDigit(Text[Index + 1]);

        /// <summary>
        /// Determines if the character can begin an identifier.
        /// </summary>
        protected virtual bool IsIdentifierStart(char ch)
            => char.IsLetter(ch) || ch == '@' || ch == '#' || ch == '_' || ch == '[' || ch == ']' || ch == '=';

        /// <summary>
        /// Determines if the character can be part of an identifier.
        /// </summary>
        protected virtual bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || char.IsDigit(ch);

        /// <summary>
        /// Reads an identifier starting at the current index.
        /// </summary>
        /// <remarks>The first character must already satisfy <see cref="IsIdentifierStart"/>.</remarks>
        protected string ReadIdentifierLiteral()
        {
            var start = Index;
            Advance();
            while (!IsAtEnd && IsIdentifierPart(Current))
            {
                Advance();
            }
            return Text.Substring(start, Index - start);
        }
    }
}

