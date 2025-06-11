using System;
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
                var parser = new ExpressionParser<float>(
                    new ExpressionTokenizer(expression),
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
}
