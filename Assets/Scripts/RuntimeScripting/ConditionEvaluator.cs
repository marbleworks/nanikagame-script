namespace RuntimeScripting
{
    /// <summary>
    /// Evaluates compound boolean expressions used in conditions.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(string expression, IGameLogic gameLogic)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return true;
            }

            try
            {
                var parser = new ConditionParser(new ConditionTokenizer(expression), gameLogic);
                return parser.ParseExpression();
            }
            catch
            {
                return false;
            }
        }
    }
}