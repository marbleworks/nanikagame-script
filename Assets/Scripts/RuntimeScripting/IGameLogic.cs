namespace RuntimeScripting
{
    /// <summary>
    /// Defines the public API for game logic accessible to the script engine.
    /// </summary>
    public interface IGameLogic
    {
        /// <summary>
        /// Executes the given parsed action.
        /// </summary>
        /// <param name="pa">Action to execute.</param>
        void ExecuteAction(ParsedAction pa);

        /// <summary>
        /// Evaluates a boolean condition expression.
        /// </summary>
        /// <param name="condition">Condition expression.</param>
        /// <returns>True if the condition is met.</returns>
        bool EvaluateCondition(string condition);

        /// <summary>
        /// Evaluates a numeric function and returns the result as float.
        /// </summary>
        /// <param name="func">Function name.</param>
        /// <param name="args">Arguments passed to the function.</param>
        /// <returns>Evaluation result.</returns>
        float EvaluateFunctionFloat(string func, System.Collections.Generic.List<string> args);
    }
}
