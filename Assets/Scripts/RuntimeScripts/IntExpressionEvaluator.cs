using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeScripting
{
    /// <summary>
    /// Evaluates arithmetic expressions returning integers.
    /// Supports +, -, *, /, parentheses and function calls.
    /// </summary>
    internal static class IntExpressionEvaluator
    {
        public static int Evaluate(string expression, GameLogic gameLogic)
        {
            // Floor the result when converting from float to int
            return (int)Math.Floor(EvaluateFloat(expression, gameLogic));
        }

        public static float EvaluateFloat(string expression, GameLogic gameLogic)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0f;

            try
            {
                var tokenizer = new Tokenizer(expression);
                var parser = new FloatParser(tokenizer, gameLogic);
                return parser.ParseFullExpression();
            }
            catch (Exception)
            {
                return 0f;
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

            public int ParseFullExpression()
            {
                var result = ParseExpression();
                Expect(TokenType.EOF);
                return result;
            }

            private int ParseExpression()
            {
                var result = ParseTerm();
                while (current.Type == TokenType.Plus || current.Type == TokenType.Minus)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseTerm();
                    result = op == TokenType.Plus ? result + right : result - right;
                }
                return result;
            }

            private int ParseTerm()
            {
                var left = ParseFactor();
                while (current.Type == TokenType.Star || current.Type == TokenType.Slash)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseFactor();
                    left = op == TokenType.Star ? left * right : left / right;
                }
                return left;
            }

            private int ParseFactor()
            {
                if (current.Type == TokenType.LParen)
                {
                    Advance();
                    var value = ParseExpression();
                    Expect(TokenType.RParen);
                    return value;
                }

                if (current.Type == TokenType.Number)
                {
                    var v = int.Parse(current.Value, CultureInfo.InvariantCulture);
                    Advance();
                    return v;
                }

                if (current.Type == TokenType.Identifier)
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
                    return gameLogic.EvaluateFunctionInt(func, args.ToArray());
                }

                throw new Exception("Unexpected token");
            }

            private string ParseArgument()
            {
                // Allow arithmetic expressions in argument positions and also
                // support plain identifiers that represent string parameters.

                if (current.Type == TokenType.Identifier)
                {
                    var id = current.Value;
                    Advance();

                    // Nested function call
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
                        int value = Enum.TryParse(id, out FunctionInt fi)
                            ? gameLogic.EvaluateFunctionInt(fi, args.ToArray())
                            : gameLogic.EvaluateFunctionInt(id, args.ToArray());

                        // Continue parsing if the result participates in an arithmetic expression
                        value = ContinueTerm(value);
                        value = ContinueExpression(value);
                        return value.ToString();
                    }

                    // Identifier without parentheses is a string argument
                    return id;
                }

                // Any other token sequence represents an arithmetic expression
                // that evaluates to an integer.
                int exprValue = ParseExpression();
                return exprValue.ToString();
            }

            private int ContinueTerm(int currentValue)
            {
                var left = currentValue;
                while (current.Type == TokenType.Star || current.Type == TokenType.Slash)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseFactor();
                    left = op == TokenType.Star ? left * right : left / right;
                }
                return left;
            }

            private int ContinueExpression(int currentValue)
            {
                var result = currentValue;
                while (current.Type == TokenType.Plus || current.Type == TokenType.Minus)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseTerm();
                    result = op == TokenType.Plus ? result + right : result - right;
                }
                return result;
            }

            private void Advance()
            {
                current = tokenizer.Next();
            }

            private void Expect(TokenType type)
            {
                if (current.Type != type)
                    throw new Exception("Expected " + type);
                Advance();
            }
        }

        private class FloatParser
        {
            private readonly Tokenizer tokenizer;
            private Token current;
            private readonly GameLogic gameLogic;

            public FloatParser(Tokenizer tokenizer, GameLogic gameLogic)
            {
                this.tokenizer = tokenizer;
                this.gameLogic = gameLogic;
                current = tokenizer.Next();
            }

            public float ParseFullExpression()
            {
                var result = ParseExpression();
                Expect(TokenType.EOF);
                return result;
            }

            private float ParseExpression()
            {
                var result = ParseTerm();
                while (current.Type == TokenType.Plus || current.Type == TokenType.Minus)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseTerm();
                    result = op == TokenType.Plus ? result + right : result - right;
                }
                return result;
            }

            private float ParseTerm()
            {
                var left = ParseFactor();
                while (current.Type == TokenType.Star || current.Type == TokenType.Slash)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseFactor();
                    left = op == TokenType.Star ? left * right : left / right;
                }
                return left;
            }

            private float ParseFactor()
            {
                if (current.Type == TokenType.LParen)
                {
                    Advance();
                    var value = ParseExpression();
                    Expect(TokenType.RParen);
                    return value;
                }

                if (current.Type == TokenType.Number)
                {
                    var v = float.Parse(current.Value, CultureInfo.InvariantCulture);
                    Advance();
                    return v;
                }

                if (current.Type == TokenType.Identifier)
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
                    return gameLogic.EvaluateFunctionFloat(func, args.ToArray());
                }

                throw new Exception("Unexpected token");
            }

            private string ParseArgument()
            {
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
                        float value = gameLogic.EvaluateFunctionFloat(id, args.ToArray());
                        value = ContinueTerm(value);
                        value = ContinueExpression(value);
                        return value.ToString(CultureInfo.InvariantCulture);
                    }

                    return id;
                }

                float exprValue = ParseExpression();
                return exprValue.ToString(CultureInfo.InvariantCulture);
            }

            private float ContinueTerm(float currentValue)
            {
                var left = currentValue;
                while (current.Type == TokenType.Star || current.Type == TokenType.Slash)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseFactor();
                    left = op == TokenType.Star ? left * right : left / right;
                }
                return left;
            }

            private float ContinueExpression(float currentValue)
            {
                var result = currentValue;
                while (current.Type == TokenType.Plus || current.Type == TokenType.Minus)
                {
                    var op = current.Type;
                    Advance();
                    var right = ParseTerm();
                    result = op == TokenType.Plus ? result + right : result - right;
                }
                return result;
            }

            private void Advance()
            {
                current = tokenizer.Next();
            }

            private void Expect(TokenType type)
            {
                if (current.Type != type)
                    throw new Exception("Expected " + type);
                Advance();
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
                    case '+': index++; return new Token(TokenType.Plus, "+");
                    case '-': index++; return new Token(TokenType.Minus, "-");
                    case '*': index++; return new Token(TokenType.Star, "*");
                    case '/': index++; return new Token(TokenType.Slash, "/");
                    case '(': index++; return new Token(TokenType.LParen, "(");
                    case ')': index++; return new Token(TokenType.RParen, ")");
                    case ',': index++; return new Token(TokenType.Comma, ",");
                }

                if (char.IsDigit(c))
                {
                    int start = index;
                    while (index < text.Length && char.IsDigit(text[index])) index++;
                    return new Token(TokenType.Number, text.Substring(start, index - start));
                }

                if (char.IsLetter(c) || c == '@' || c == '#' || c == '[' || c == ']' || c == '=')
                {
                    int start = index;
                    while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index]=='_' || text[index]=='@' || text[index]=='#' || text[index]=='[' || text[index]==']' || text[index]=='='))
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
        }

        private enum TokenType
        {
            EOF,
            Number,
            Identifier,
            Plus,
            Minus,
            Star,
            Slash,
            LParen,
            RParen,
            Comma
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
