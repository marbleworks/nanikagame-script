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
                var tokenizer = new Tokenizer(expression);
                var parser = new Parser(tokenizer, gameLogic);
                return parser.ParseExpression();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private class Parser
        {
            private readonly Tokenizer tokenizer;
            private Token current;
            private readonly GameLogic gameLogic;

            public Parser(Tokenizer tokenizer, GameLogic gameLogic)
            {
                this.tokenizer = tokenizer;
                this.gameLogic = gameLogic;
                current = tokenizer.Next();
            }

            public bool ParseExpression()
            {
                var result = ParseOr();
                Expect(TokenType.EOF);
                return result;
            }

            private bool ParseOr()
            {
                var left = ParseAnd();
                while (current.Type == TokenType.Or)
                {
                    Advance();
                    var right = ParseAnd();
                    left = left || right;
                }
                return left;
            }

            private bool ParseAnd()
            {
                var left = ParseUnary();
                while (current.Type == TokenType.And)
                {
                    Advance();
                    var right = ParseUnary();
                    left = left && right;
                }
                return left;
            }

            private bool ParseUnary()
            {
                if (current.Type == TokenType.Not)
                {
                    Advance();
                    return !ParseUnary();
                }
                return ParsePrimary();
            }

            private bool ParsePrimary()
            {
                if (current.Type == TokenType.LParen)
                {
                    Advance();
                    var result = ParseOr();
                    Expect(TokenType.RParen);
                    return result;
                }

                var leftValue = ParseValue();
                if (IsComparisonOperator(current.Type))
                {
                    var op = current.Type;
                    Advance();
                    var rightValue = ParseValue();
                    return Compare(leftValue, op, rightValue);
                }

                // If no comparison, non-zero is true
                return leftValue != 0;
            }

            private int ParseValue()
            {
                if (current.Type == TokenType.Number)
                {
                    var v = int.Parse(current.Value, CultureInfo.InvariantCulture);
                    Advance();
                    return v;
                }
                else if (current.Type == TokenType.Identifier)
                {
                    var func = current.Value;
                    Advance();
                    Expect(TokenType.LParen);
                    var args = new List<string>();
                    if (current.Type != TokenType.RParen)
                    {
                        args.Add(ParseArgument());
                        while (current.Type == TokenType.Comma)
                        {
                            Advance();
                            args.Add(ParseArgument());
                        }
                    }
                    Expect(TokenType.RParen);
                    if (Enum.TryParse(func, out FunctionInt f))
                        return gameLogic.EvaluateFunctionInt(f, args.ToArray());
                    return gameLogic.EvaluateFunctionInt(func, args.ToArray());
                }

                throw new Exception("Unexpected token" + current.Type);
            }

            private string ParseArgument()
            {
                if (current.Type == TokenType.Number)
                {
                    var val = current.Value;
                    Advance();
                    return val;
                }

                if (current.Type == TokenType.Identifier)
                {
                    var id = current.Value;
                    Advance();
                    if (current.Type == TokenType.LParen)
                    {
                        Expect(TokenType.LParen);
                        var args = new List<string>();
                        if (current.Type != TokenType.RParen)
                        {
                            args.Add(ParseArgument());
                            while (current.Type == TokenType.Comma)
                            {
                                Advance();
                                args.Add(ParseArgument());
                            }
                        }
                        Expect(TokenType.RParen);
                        int value;
                        if (Enum.TryParse(id, out FunctionInt fi))
                            value = gameLogic.EvaluateFunctionInt(fi, args.ToArray());
                        else
                            value = gameLogic.EvaluateFunctionInt(id, args.ToArray());
                        return value.ToString();
                    }
                    return id;
                }

                throw new Exception("Invalid argument");
            }

            private static bool Compare(int left, TokenType op, int right)
            {
                switch (op)
                {
                    case TokenType.Less: return left < right;
                    case TokenType.LessEqual: return left <= right;
                    case TokenType.Greater: return left > right;
                    case TokenType.GreaterEqual: return left >= right;
                    case TokenType.Equal: return left == right;
                    default: return false;
                }
            }

            private void Advance()
            {
                current = tokenizer.Next();
            }

            private void Expect(TokenType type)
            {
                if (current.Type != type)
                {
                    throw new Exception("Expected " + type + " but got " + current.Type);
                }
                Advance();
            }

            private static bool IsComparisonOperator(TokenType type)
            {
                return type == TokenType.Less || type == TokenType.LessEqual ||
                       type == TokenType.Greater || type == TokenType.GreaterEqual ||
                       type == TokenType.Equal;
            }
        }

        private class Tokenizer
        {
            private readonly string text;
            private int index;

            public Tokenizer(string text)
            {
                this.text = text;
            }

            public Token Next()
            {
                SkipWhite();
                if (index >= text.Length)
                    return new Token(TokenType.EOF, string.Empty);

                char c = text[index];
                switch (c)
                {
                    case '(': index++; return new Token(TokenType.LParen, "(");
                    case ')': index++; return new Token(TokenType.RParen, ")");
                    case ',': index++; return new Token(TokenType.Comma, ",");
                    case '!': index++; return new Token(TokenType.Not, "!");
                }

                if (c == '&' && Peek(1) == '&')
                {
                    index += 2; return new Token(TokenType.And, "&&");
                }
                if (c == '|' && Peek(1) == '|')
                {
                    index += 2; return new Token(TokenType.Or, "||");
                }
                if (c == '<')
                {
                    if (Peek(1) == '=')
                    {
                        index += 2; return new Token(TokenType.LessEqual, "<=");
                    }
                    index++; return new Token(TokenType.Less, "<");
                }
                if (c == '>')
                {
                    if (Peek(1) == '=')
                    {
                        index += 2; return new Token(TokenType.GreaterEqual, ">=");
                    }
                    index++; return new Token(TokenType.Greater, ">");
                }
                if (c == '=')
                {
                    if (Peek(1) == '=')
                    {
                        index += 2; return new Token(TokenType.Equal, "==");
                    }
                }

                if (char.IsDigit(c))
                {
                    int start = index;
                    while (index < text.Length && char.IsDigit(text[index])) index++;
                    return new Token(TokenType.Number, text.Substring(start, index - start));
                }

                if (char.IsLetter(c) || c == '@' || c == '#')
                {
                    int start = index;
                    while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_' || text[index]=='@' || text[index]=='#' || text[index]=='[' || text[index]==']' || text[index]=='='))
                    {
                        index++;
                    }
                    return new Token(TokenType.Identifier, text.Substring(start, index - start));
                }

                throw new Exception("Invalid character");
            }

            private void SkipWhite()
            {
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            }

            private char Peek(int offset)
            {
                int i = index + offset;
                if (i >= text.Length) return '\0';
                return text[i];
            }
        }

        private enum TokenType
        {
            EOF,
            Identifier,
            Number,
            LParen,
            RParen,
            Comma,
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