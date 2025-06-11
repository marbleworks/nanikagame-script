using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeScripting
{
    /// <summary>
    /// Recursive-descent parser for boolean condition expressions.
    /// </summary>
    internal sealed class ConditionParser
    {
        private ConditionToken _current;
        private readonly ConditionTokenizer _tokenizer;
        private readonly IGameLogic _gameLogic;

        public ConditionParser(ConditionTokenizer tokenizer, IGameLogic gameLogic)
        {
            _tokenizer = tokenizer;
            _gameLogic = gameLogic;
            _current = _tokenizer.Next();
        }

        public bool ParseExpression()
        {
            var result = ParseOr();
            Expect(ConditionTokenType.Eof);
            return result;
        }

        private bool ParseOr()
        {
            var result = ParseAnd();
            while (Match(ConditionTokenType.Or))
            {
                var right = ParseAnd();
                result |= right;
            }

            return result;
        }

        private bool ParseAnd()
        {
            var result = ParseUnary();
            while (Match(ConditionTokenType.And))
            {
                var right = ParseUnary();
                result &= right;
            }

            return result;
        }

        private bool ParseUnary()
        {
            if (Match(ConditionTokenType.Not))
            {
                return !ParseUnary();
            }

            return ParsePrimary();
        }

        private bool ParsePrimary()
        {
            if (Match(ConditionTokenType.LParen))
            {
                var inner = ParseOr();
                Expect(ConditionTokenType.RParen);
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
                ConditionTokenType.Less => left < right,
                ConditionTokenType.LessEqual => left <= right,
                ConditionTokenType.Greater => left > right,
                ConditionTokenType.GreaterEqual => left >= right,
                ConditionTokenType.Equal => Math.Abs(left - right) < float.Epsilon,
                _ => throw new InvalidOperationException($"Unknown operator {op}")
            };
        }

        private float ParseArithmetic()
        {
            var result = ParseTerm();
            while (_current.Type is ConditionTokenType.Plus or ConditionTokenType.Minus)
            {
                var op = _current.Type;
                Advance();
                var right = ParseTerm();
                result = op == ConditionTokenType.Plus ? result + right : result - right;
            }

            return result;
        }

        private float ParseTerm()
        {
            var result = ParseFactor();
            while (_current.Type is ConditionTokenType.Star or ConditionTokenType.Slash)
            {
                var op = _current.Type;
                Advance();
                var right = ParseFactor();
                result = op == ConditionTokenType.Star ? result * right : result / right;
            }

            return result;
        }

        private float ParseFactor()
        {
            if (_current.Type == ConditionTokenType.Number)
            {
                var value = float.Parse(_current.Value, CultureInfo.InvariantCulture);
                Advance();
                return value;
            }

            if (_current.Type == ConditionTokenType.Identifier)
            {
                var name = _current.Value;
                Advance();

                if (string.Equals(name, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return 1f;
                }

                if (string.Equals(name, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return 0f;
                }

                Expect(ConditionTokenType.LParen);
                var args = ParseArguments();
                return _gameLogic.EvaluateFunctionFloat(name, args);
            }

            if (Match(ConditionTokenType.LParen))
            {
                var value = ParseArithmetic();
                Expect(ConditionTokenType.RParen);
                return value;
            }

            throw new InvalidOperationException($"Unexpected token {_current.Type}");
        }

        private List<string> ParseArguments()
        {
            var args = new List<string>();
            if (_current.Type != ConditionTokenType.RParen)
            {
                args.Add(ParseArgument());
                while (Match(ConditionTokenType.Comma))
                    args.Add(ParseArgument());
            }

            Expect(ConditionTokenType.RParen);
            return args;
        }

        private string ParseArgument()
        {
            if (_current.Type is ConditionTokenType.String or ConditionTokenType.Number)
            {
                var val = _current.Value;
                Advance();
                return val;
            }

            if (_current.Type == ConditionTokenType.Identifier)
            {
                var id = _current.Value;
                Advance();
                if (Match(ConditionTokenType.LParen))
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

        private bool Match(ConditionTokenType type)
        {
            if (_current.Type != type)
            {
                return false;
            }
            Advance();
            return true;
        }

        private void Expect(ConditionTokenType type)
        {
            if (_current.Type != type)
            {
                throw new InvalidOperationException($"Expected {type} but got {_current.Type}");
            }
            Advance();
        }

        private static bool IsComparisonOperator(ConditionTokenType type)
        {
            return type is ConditionTokenType.Less or ConditionTokenType.LessEqual or ConditionTokenType.Greater or ConditionTokenType.GreaterEqual
                or ConditionTokenType.Equal;
        }
    }
}
