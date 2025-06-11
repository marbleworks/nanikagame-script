using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace RuntimeScripting
{
    /// <summary>
    /// Placeholder for game-specific logic. In a real project these methods
    /// would interact with the rest of the game systems.
    /// </summary>
    public class GameLogic : IGameLogic
    {
        private readonly Dictionary<string, Action<GameLogic, ActionParameter>> _actions = new();
        private readonly Dictionary<string, Func<GameLogic, ActionParameter, float>> _functions = new();

        public GameLogic()
        {
            RegisterDefaultFunctions();
        }

        private void RegisterDefaultFunctions()
        {
            RegisterFunction(nameof(RandomInt),
                (logic, parameter) => RandomInt(
                    ParseIntArg(parameter, 0),
                    ParseIntArg(parameter, 1)));

            RegisterFunction(nameof(RandomFloat),
                (logic, parameter) => RandomFloat(
                    ParseFloatArg(parameter, 0),
                    ParseFloatArg(parameter, 1)));

            RegisterFunction(nameof(Double),
                (logic, parameter) => Double(
                    ParseFloatArg(parameter, 0)));

            RegisterFunction(nameof(Pow),
                (logic, parameter) => Pow(
                    ParseFloatArg(parameter, 0),
                    ParseFloatArg(parameter, 1)));

            RegisterFunction(nameof(Sqrt),
                (logic, parameter) => Sqrt(
                    ParseFloatArg(parameter, 0)));

            RegisterFunction(nameof(Mod),
                (logic, parameter) => Mod(
                    ParseFloatArg(parameter, 0),
                    ParseFloatArg(parameter, 1)));
        }

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
        
        public void RegisterFunction(string name, Func<GameLogic, ActionParameter, bool> func)
        {
            RegisterFunction(name, (logic, parameter) => func(logic, parameter) ? 1f : 0f);
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

        /// <inheritdoc/>
        public void ExecuteAction(ParsedAction pa)
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

        public int ParseIntArg(ActionParameter param, int index)
            => (int) Math.Floor(ParseFloatArg(param, index));

        private float ParseFloatArg(string arg) =>
            float.TryParse(arg, out var val) ? val : ExpressionEvaluator.EvaluateFloat(arg, this);

        public float ParseFloatArg(ActionParameter param, int index)
            => index >= 0 && index < param.Args.Count ? ParseFloatArg(param.Args[index]) : 0f;

        private static int RandomInt(int min, int max) => Random.Range(min, max);

        private static float RandomFloat(float min, float max) => Random.Range(min, max);

        private static float Double(float value) => value * 2f;
        private static float Pow(float value, float power) => (float) Math.Pow(value, power);
        private static float Sqrt(float value) => (float) Math.Sqrt(value);
        private static float Mod(float value, float mod) => value % mod;

    }
}
