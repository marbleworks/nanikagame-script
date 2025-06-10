using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeScripting
{
    /// <summary>
    /// Provides methods to evaluate arithmetic expressions returning integers.
    /// Internally uses a generic parser for float evaluation and then floors the result.
    /// </summary>
    internal static class ExpressionEvaluator
    {
        /// <summary>
        /// Evaluates the given expression and returns the floored integer result.
        /// </summary>
        public static int Evaluate(string expression, GameLogic gameLogic)
        {
            return (int) Math.Floor(EvaluateFloat(expression, gameLogic));
        }

        /// <summary>
        /// Evaluates the given expression as a float.
        /// Returns 0 on null/empty input or parsing errors.
        /// </summary>
        public static float EvaluateFloat(string expression, GameLogic gameLogic)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return 0f;
            }

            try
            {
                var parser = new Parser<float>(
                    expression,
                    s => float.Parse(s, CultureInfo.InvariantCulture),
                    gameLogic.EvaluateFunctionFloat,
                    (a, b) => a + b,
                    (a, b) => a - b,
                    (a, b) => a * b,
                    (a, b) => a / b);

                return parser.Parse();
            }
            catch
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// Generic recursive-descent parser for arithmetic expressions.
    /// Supports +, -, *, /, parentheses, numeric literals, string literals, and function calls.
    /// T represents the numeric return type (e.g., float).
    /// </summary>
    internal class Parser<T>
    {
        private readonly Tokenizer _tokenizer;
        private readonly Func<string, T> _parseNumber;
        private readonly Func<string, List<string>, T> _evalFunc;
        private readonly Func<T, T, T> _add;
        private readonly Func<T, T, T> _sub;
        private readonly Func<T, T, T> _mul;
        private readonly Func<T, T, T> _div;
        private Token _current;

        public Parser(
            string expression,
            Func<string, T> parseNumber,
            Func<string, List<string>, T> evalFunc,
            Func<T, T, T> add,
            Func<T, T, T> sub,
            Func<T, T, T> mul,
            Func<T, T, T> div)
        {
            _tokenizer = new Tokenizer(expression);
            _parseNumber = parseNumber;
            _evalFunc = evalFunc;
            _add = add;
            _sub = sub;
            _mul = mul;
            _div = div;
            _current = _tokenizer.Next();
        }

        /// <summary>
        /// Parses the entire expression and returns the result.
        /// </summary>
        public T Parse()
        {
            var result = ParseExpression();
            Expect(TokenType.Eof);
            return result;
        }

        private T ParseExpression()
        {
            var result = ParseTerm();
            while (_current.Type is TokenType.Plus or TokenType.Minus)
            {
                var op = _current.Type;
                Advance();
                var right = ParseTerm();
                result = op == TokenType.Plus ? _add(result, right) : _sub(result, right);
            }

            return result;
        }

        private T ParseTerm()
        {
            var result = ParseFactor();
            while (_current.Type is TokenType.Star or TokenType.Slash)
            {
                var op = _current.Type;
                Advance();
                var right = ParseFactor();
                result = op == TokenType.Star ? _mul(result, right) : _div(result, right);
            }

            return result;
        }

        private T ParseFactor()
        {
            return _current.Type switch
            {
                TokenType.LParen => ParseParenthesized(),
                TokenType.Number => ParseNumber(),
                TokenType.Identifier => ParseFunction(),
                _ => throw new InvalidOperationException($"Unexpected token: {_current.Type}")
            };
        }

        private T ParseParenthesized()
        {
            Advance();
            var value = ParseExpression();
            Expect(TokenType.RParen);
            return value;
        }

        private T ParseNumber()
        {
            var value = _parseNumber(_current.Value);
            Advance();
            return value;
        }

        private T ParseFunction()
        {
            var name = _current.Value;
            Advance();
            Expect(TokenType.LParen);
            var args = ParseArguments();
            Expect(TokenType.RParen);
            return _evalFunc(name, args);
        }

        private List<string> ParseArguments()
        {
            var args = new List<string>();
            while (_current.Type != TokenType.RParen)
            {
                if (_current.Type == TokenType.String)
                {
                    args.Add(_current.Value);
                    Advance();
                }
                else
                {
                    var exprValue = ParseExpression();
                    args.Add(exprValue.ToString());
                }

                if (_current.Type == TokenType.Comma)
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }

            return args;
        }

        private void Advance()
        {
            _current = _tokenizer.Next();
        }

        private void Expect(TokenType type)
        {
            if (_current.Type != type)
            {
                throw new InvalidOperationException($"Expected {type}, but got {_current.Type}");
            }

            Advance();
        }
    }

    /// <summary>
    /// Simple tokenizer for arithmetic expressions.
    /// Recognizes numbers, identifiers, string literals, operators, and parentheses.
    /// </summary>
    internal class Tokenizer
    {
        private readonly string _text;
        private int _position;

        public Tokenizer(string text)
        {
            (_text, _position) = (text, 0);
        }

        public Token Next()
        {
            SkipWhitespace();

            if (_position >= _text.Length)
            {
                return new Token(TokenType.Eof, string.Empty);
            }

            var ch = _text[_position];
            return ch switch
            {
                '+' => ReturnToken(TokenType.Plus, "+"),
                '-' => ReturnToken(TokenType.Minus, "-"),
                '*' => ReturnToken(TokenType.Star, "*"),
                '/' => ReturnToken(TokenType.Slash, "/"),
                '(' => ReturnToken(TokenType.LParen, "("),
                ')' => ReturnToken(TokenType.RParen, ")"),
                ',' => ReturnToken(TokenType.Comma, ","),
                '"' => ReadString(),
                _ when char.IsDigit(ch) || (ch == '.' && PeekDigit()) => ReadNumber(),
                _ when char.IsLetter(ch) || IsIdentifierStart(ch) => ReadIdentifier(),
                _ => throw new InvalidOperationException($"Invalid character at position {_position}: '{ch}'")
            };
        }

        private Token ReturnToken(TokenType type, string value)
        {
            _position++;
            return new Token(type, value);
        }

        private Token ReadString()
        {
            _position++;
            var start = _position;
            while (_position < _text.Length && _text[_position] != '"')
            {
                if (_text[_position] == '\\' && _position + 1 < _text.Length)
                {
                    _position += 2;
                }
                else
                {
                    _position++;
                }
            }

            var content = _text[start.._position];
            if (_position < _text.Length && _text[_position] == '"')
            {
                _position++;
            }

            return new Token(TokenType.String, content);
        }

        private Token ReadNumber()
        {
            var start = _position;
            var hasDot = false;
            if (_text[_position] == '.')
            {
                hasDot = true;
                _position++;
            }

            while (_position < _text.Length)
            {
                var ch = _text[_position];
                if (char.IsDigit(ch))
                {
                    _position++;
                }
                else if (ch == '.' && !hasDot)
                {
                    hasDot = true;
                    _position++;
                }
                else
                {
                    break;
                }
            }

            return new Token(TokenType.Number, _text[start.._position]);
        }

        private bool PeekDigit()
        {
            return _position + 1 < _text.Length && char.IsDigit(_text[_position + 1]);
        }

        private Token ReadIdentifier()
        {
            var start = _position;
            while (_position < _text.Length &&
                   (char.IsLetterOrDigit(_text[_position]) || IsIdentifierPart(_text[_position])))
            {
                _position++;
            }

            return new Token(TokenType.Identifier, _text[start.._position]);
        }

        private static bool IsIdentifierStart(char ch)
        {
            return ch is '@' or '#' or '[' or ']' or '=' or '_';
        }

        private static bool IsIdentifierPart(char ch)
        {
            return IsIdentifierStart(ch) || char.IsLetterOrDigit(ch);
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }
    }

    internal enum TokenType
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

    internal readonly struct Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public Token(TokenType type, string value)
        {
            (Type, Value) = (type, value);
        }
    }
}