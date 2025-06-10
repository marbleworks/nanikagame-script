using System;
using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Placeholder for game-specific logic. In a real project these methods
    /// would interact with the rest of the game systems.
    /// </summary>
    public class GameLogic
    {
        private readonly Dictionary<string, Action<GameLogic, ActionParameter>> _actions = new();
        private readonly Dictionary<string, Func<GameLogic, ActionParameter, float>> _functions = new();

        /// <summary>
        /// Registers a custom action that can be invoked from scripts.
        /// </summary>
        /// <param name="name">Function name used in the DSL.</param>
        /// <param name="action">Delegate to execute when called.</param>
        public void RegisterAction(string name, Action<GameLogic, ActionParameter> action)
        {
            if (string.IsNullOrEmpty(name) || action == null) return;
            _actions[name] = action;
        }

        /// <summary>
        /// Registers a custom numeric function for expression evaluation.
        /// </summary>
        /// <param name="name">Function name used in the DSL.</param>
        /// <param name="func">Delegate that returns a numeric result.</param>
        public void RegisterFunction(string name, Func<GameLogic, ActionParameter, float> func)
        {
            if (string.IsNullOrEmpty(name) || func == null) return;
            _functions[name] = func;
        }

        public float EvaluateFunctionFloat(string func, List<string> args)
        {
            var param = CreateParameter(func, args);

            return _functions.TryGetValue(func, out var custom) ? custom(this, param) : 0f;
        }

        /// <summary>
        /// Evaluates a boolean condition string using the built-in parser.
        /// </summary>
        /// <param name="condition">Condition expression.</param>
        /// <returns>True if the expression evaluates to true; otherwise, false.</returns>
        public bool EvaluateCondition(string condition)
        {
            return ConditionEvaluator.Evaluate(condition, this);
        }

        private static ActionParameter CreateParameter(ParsedAction pa)
        {
            return CreateParameter(pa.FunctionName, pa.Args);
        }

        private static ActionParameter CreateParameter(string func, List<string> args)
        {
            var param = new ActionParameter
            {
                FunctionName = func,
                Args = args
            };

            return param;
        }

        internal void ExecuteAction(ParsedAction pa)
        {
            ExecuteAction(CreateParameter(pa));
        }

        private void ExecuteAction(ActionParameter param)
        {
            if (_actions.TryGetValue(param.FunctionName, out var action))
            {
                action(this, param);
            }
        }

        private int ParseIntArg(string arg)
            => int.TryParse(arg, out var val)
                ? val
                : IntExpressionEvaluator.Evaluate(arg, this);
        
        public int ParseIntArg(ActionParameter param, int index)
            => index >= 0 && index < param.Args.Count ? ParseIntArg(param.Args[index]) : 0;

        private float ParseFloatArg(string arg) =>
            float.TryParse(arg, out var val) ? val : IntExpressionEvaluator.EvaluateFloat(arg, this);
        
        public float ParseFloatArg(ActionParameter param, int index)
            => index >= 0 && index < param.Args.Count ? ParseFloatArg(param.Args[index]) : 0f;
    }
}
