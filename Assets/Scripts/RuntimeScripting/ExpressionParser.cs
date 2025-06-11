using System;
using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Generic recursive-descent parser for arithmetic expressions.
    /// </summary>
    internal sealed class ExpressionParser<T>
    {
        private readonly ExpressionTokenizer _tokenizer;
        private readonly Func<string, T> _parseNumber;
        private readonly Func<string, List<string>, T> _evalFunc;
        private readonly Func<T, T, T> _add;
        private readonly Func<T, T, T> _sub;
        private readonly Func<T, T, T> _mul;
        private readonly Func<T, T, T> _div;
        private ExprToken _current;

        public ExpressionParser(
            ExpressionTokenizer tokenizer,
            Func<string, T> parseNumber,
            Func<string, List<string>, T> evalFunc,
            Func<T, T, T> add,
            Func<T, T, T> sub,
            Func<T, T, T> mul,
            Func<T, T, T> div)
        {
            _tokenizer = tokenizer;
            _parseNumber = parseNumber;
            _evalFunc = evalFunc;
            _add = add;
            _sub = sub;
            _mul = mul;
            _div = div;
            _current = _tokenizer.Next();
        }

        public T Parse()
        {
            var result = ParseExpression();
            Expect(ExprTokenType.Eof);
            return result;
        }

        private T ParseExpression()
        {
            var result = ParseTerm();
            while (_current.Type is ExprTokenType.Plus or ExprTokenType.Minus)
            {
                var op = _current.Type;
                Advance();
                var right = ParseTerm();
                result = op == ExprTokenType.Plus ? _add(result, right) : _sub(result, right);
            }
            return result;
        }

        private T ParseTerm()
        {
            var result = ParseFactor();
            while (_current.Type is ExprTokenType.Star or ExprTokenType.Slash)
            {
                var op = _current.Type;
                Advance();
                var right = ParseFactor();
                result = op == ExprTokenType.Star ? _mul(result, right) : _div(result, right);
            }
            return result;
        }

        private T ParseFactor()
        {
            return _current.Type switch
            {
                ExprTokenType.LParen => ParseParenthesized(),
                ExprTokenType.Number => ParseNumber(),
                ExprTokenType.Identifier => ParseFunction(),
                _ => throw new InvalidOperationException($"Unexpected token: {_current.Type}")
            };
        }

        private T ParseParenthesized()
        {
            Advance();
            var value = ParseExpression();
            Expect(ExprTokenType.RParen);
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
            Expect(ExprTokenType.LParen);
            var args = ParseArguments();
            Expect(ExprTokenType.RParen);
            return _evalFunc(name, args);
        }

        private List<string> ParseArguments()
        {
            var args = new List<string>();
            while (_current.Type != ExprTokenType.RParen)
            {
                if (_current.Type == ExprTokenType.String)
                {
                    args.Add(_current.Value);
                    Advance();
                }
                else
                {
                    var exprValue = ParseExpression();
                    args.Add(exprValue.ToString());
                }

                if (_current.Type == ExprTokenType.Comma)
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

        private void Advance() => _current = _tokenizer.Next();

        private void Expect(ExprTokenType type)
        {
            if (_current.Type != type)
            {
                throw new InvalidOperationException($"Expected {type}, but got {_current.Type}");
            }
            Advance();
        }
    }
}
