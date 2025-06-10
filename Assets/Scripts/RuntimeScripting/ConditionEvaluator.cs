using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeScripting
{
    /// <summary>
    /// Evaluates compound boolean expressions used in conditions.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(string expression, GameLogic gameLogic)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return true;
            }

            try
            {
                var parser = new Parser(new Tokenizer(expression), gameLogic);
                return parser.ParseExpression();
            }
            catch
            {
                return false;
            }
        }

        private class Parser
        {
            private Token _current;
            private readonly Tokenizer _tokenizer;
            private readonly GameLogic _gameLogic;

            public Parser(Tokenizer tokenizer, GameLogic gameLogic)
            {
                _tokenizer = tokenizer;
                _gameLogic = gameLogic;
                _current = tokenizer.Next();
            }

            public bool ParseExpression()
            {
                var result = ParseOr();
                Expect(TokenType.Eof);
                return result;
            }

            private bool ParseOr()
            {
                var result = ParseAnd();
                while (Match(TokenType.Or))
                {
                    var right = ParseAnd();
                    result |= right;
                }

                return result;
            }

            private bool ParseAnd()
            {
                var result = ParseUnary();
                while (Match(TokenType.And))
                {
                    var right = ParseUnary();
                    result &= right;
                }

                return result;
            }

            private bool ParseUnary()
            {
                if (Match(TokenType.Not))
                {
                    return !ParseUnary();
                }

                return ParsePrimary();
            }

            private bool ParsePrimary()
            {
                if (Match(TokenType.LParen))
                {
                    var inner = ParseOr();
                    Expect(TokenType.RParen);
                    return inner;
                }

                var left = ParseArithmetic();
                if (!IsComparisonOperator(_current.Type))
                {
                    return Math.Abs(left) > float.Epsilon;
                }

                var op = _current.Type;
                Advance();
                var right = ParseArithmetic();
                return op switch
                {
                    TokenType.Less => left < right,
                    TokenType.LessEqual => left <= right,
                    TokenType.Greater => left > right,
                    TokenType.GreaterEqual => left >= right,
                    TokenType.Equal => Math.Abs(left - right) < float.Epsilon,
                    _ => throw new InvalidOperationException($"Unknown operator {op}")
                };
            }

            private float ParseArithmetic()
            {
                var result = ParseTerm();
                while (_current.Type is TokenType.Plus or TokenType.Minus)
                {
                    var op = _current.Type;
                    Advance();
                    var right = ParseTerm();
                    result = op == TokenType.Plus ? result + right : result - right;
                }

                return result;
            }

            private float ParseTerm()
            {
                var result = ParseFactor();
                while (_current.Type is TokenType.Star or TokenType.Slash)
                {
                    var op = _current.Type;
                    Advance();
                    var right = ParseFactor();
                    result = op == TokenType.Star ? result * right : result / right;
                }

                return result;
            }

            private float ParseFactor()
            {
                if (_current.Type == TokenType.Number)
                {
                    var value = float.Parse(_current.Value, CultureInfo.InvariantCulture);
                    Advance();
                    return value;
                }

                if (_current.Type == TokenType.Identifier)
                {
                    var name = _current.Value;
                    Advance();

                    // allow "true"/"false" literals without parentheses
                    if (string.Equals(name, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return 1f;
                    }

                    if (string.Equals(name, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0f;
                    }

                    Expect(TokenType.LParen);
                    var args = ParseArguments();
                    return _gameLogic.EvaluateFunctionFloat(name, args);
                }

                if (Match(TokenType.LParen))
                {
                    var value = ParseArithmetic();
                    Expect(TokenType.RParen);
                    return value;
                }

                throw new InvalidOperationException($"Unexpected token {_current.Type}");
            }

            private List<string> ParseArguments()
            {
                var args = new List<string>();
                if (_current.Type != TokenType.RParen)
                {
                    args.Add(ParseArgument());
                    while (Match(TokenType.Comma))
                        args.Add(ParseArgument());
                }

                Expect(TokenType.RParen);
                return args;
            }

            private string ParseArgument()
            {
                if (_current.Type is TokenType.String or TokenType.Number)
                {
                    var val = _current.Value;
                    Advance();
                    return val;
                }

                if (_current.Type == TokenType.Identifier)
                {
                    var id = _current.Value;
                    Advance();
                    if (Match(TokenType.LParen))
                    {
                        var innerArgs = ParseArguments();
                        var val = _gameLogic.EvaluateFunctionFloat(id, innerArgs);
                        return val.ToString(CultureInfo.InvariantCulture);
                    }

                    return id;
                }

                throw new InvalidOperationException($"Invalid argument {_current.Type}");
            }

            private void Advance() => _current = _tokenizer.Next();

            private bool Match(TokenType type)
            {
                if (_current.Type != type)
                {
                    return false;
                }

                Advance();
                return true;
            }

            private void Expect(TokenType type)
            {
                if (_current.Type != type)
                {
                    throw new InvalidOperationException($"Expected {type} but got {_current.Type}");
                }

                Advance();
            }

            private static bool IsComparisonOperator(TokenType type)
            {
                return type is TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual
                    or TokenType.Equal;
            }
        }

        private class Tokenizer
        {
            private readonly string _text;
            private int _index;

            public Tokenizer(string text) => _text = text;

            public Token Next()
            {
                SkipWhitespace();
                if (_index >= _text.Length)
                {
                    return new Token(TokenType.Eof, string.Empty);
                }

                var c = _text[_index];
                switch (c)
                {
                    case '(':
                        _index++;
                        return new Token(TokenType.LParen, "(");
                    case ')':
                        _index++;
                        return new Token(TokenType.RParen, ")");
                    case ',':
                        _index++;
                        return new Token(TokenType.Comma, ",");
                    case '!':
                        _index++;
                        return new Token(TokenType.Not, "!");
                    case '+':
                        _index++;
                        return new Token(TokenType.Plus, "+");
                    case '-':
                        _index++;
                        return new Token(TokenType.Minus, "-");
                    case '*':
                        _index++;
                        return new Token(TokenType.Star, "*");
                    case '/':
                        _index++;
                        return new Token(TokenType.Slash, "/");
                    case '"':
                        _index++;
                        var start = _index;
                        while (_index < _text.Length && _text[_index] != '"')
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
                        if (_index < _text.Length && _text[_index] == '"')
                        {
                            _index++;
                        }

                        return new Token(TokenType.String, str);
                }

                if (c == '&' && Peek(1) == '&')
                {
                    _index += 2;
                    return new Token(TokenType.And, "&&");
                }

                if (c == '|' && Peek(1) == '|')
                {
                    _index += 2;
                    return new Token(TokenType.Or, "||");
                }

                if (c == '<')
                {
                    if (Peek(1) == '=')
                    {
                        _index += 2;
                        return new Token(TokenType.LessEqual, "<=");
                    }

                    _index++;
                    return new Token(TokenType.Less, "<");
                }

                if (c == '>')
                {
                    if (Peek(1) == '=')
                    {
                        _index += 2;
                        return new Token(TokenType.GreaterEqual, ">=");
                    }

                    _index++;
                    return new Token(TokenType.Greater, ">");
                }

                if (c == '=' && Peek(1) == '=')
                {
                    _index += 2;
                    return new Token(TokenType.Equal, "==");
                }

                if (char.IsDigit(c) || (c == '.' && _index + 1 < _text.Length && char.IsDigit(_text[_index + 1])))
                {
                    var start = _index;
                    var hasDot = false;
                    if (c == '.')
                    {
                        hasDot = true;
                        _index++;
                    }

                    while (_index < _text.Length)
                    {
                        var nc = _text[_index];
                        if (char.IsDigit(nc))
                        {
                            _index++;
                        }
                        else if (nc == '.' && !hasDot)
                        {
                            hasDot = true;
                            _index++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return new Token(TokenType.Number, _text.Substring(start, _index - start));
                }

                if (char.IsLetter(c) || c == '@' || c == '#' || c == '_' || c == '[' || c == ']' || c == '=')
                {
                    var start = _index;
                    while (_index < _text.Length &&
                           (char.IsLetterOrDigit(_text[_index]) || "_@#[]=".IndexOf(_text[_index]) >= 0))
                    {
                        _index++;
                    }

                    return new Token(TokenType.Identifier, _text.Substring(start, _index - start));
                }

                throw new InvalidOperationException($"Invalid character '{c}' at position {_index}");
            }

            private void SkipWhitespace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                {
                    _index++;
                }
            }

            private char Peek(int offset)
            {
                var pos = _index + offset;
                return pos < _text.Length ? _text[pos] : '\0';
            }
        }

        private enum TokenType
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

        private readonly struct Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            public Token(TokenType type, string value)
            {
                Type = type;
                Value = value;
            }
        }
    }
}