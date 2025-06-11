namespace RuntimeScripting
{
    /// <summary>
    /// Provides common tokenizer utilities for derived tokenizers.
    /// </summary>
    internal abstract class TokenizerBase
    {
        protected readonly string Text;
        protected int Index;

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
    }
}

