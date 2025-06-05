using System;
using System.Collections;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Handles periodic execution of actions.
    /// </summary>
    public class ScheduledAction
    {
        private readonly ParsedAction parsed;
        private readonly RuntimeTextScriptController controller;
        private float elapsed;
        private float nextTime;

        public ScheduledAction(ParsedAction parsed, RuntimeTextScriptController controller)
        {
            this.parsed = parsed;
            this.controller = controller;
            nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
        }

        /// <summary>
        /// Coroutine execution handling intervals automatically.
        /// </summary>
        public IEnumerator ExecuteCoroutine()
        {
            elapsed = 0f;
            nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
            while (parsed.Period <= 0 || elapsed < parsed.Period)
            {
                yield return new WaitForSeconds(nextTime);
                elapsed += nextTime;
                if (parsed.Period > 0 && elapsed > parsed.Period)
                    yield break;

                if (string.IsNullOrEmpty(parsed.CanExecuteRaw) || ConditionEvaluator.Evaluate(parsed.CanExecuteRaw, controller.GameLogic))
                {
                    var param = controller.CreateParameter(parsed);
                    controller.ExecuteActionImmediately(param);
                }
                nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
            }
        }

        private float EvaluateInterval()
        {
            if (string.IsNullOrEmpty(parsed.IntervalFuncRaw))
                return parsed.Interval;

            var s = parsed.IntervalFuncRaw.Trim();
            int open = s.IndexOf('(');
            int close = s.LastIndexOf(')');
            if (open > 0 && close > open)
            {
                var func = s.Substring(0, open);
                var argsPart = s.Substring(open + 1, close - open - 1);
                var args = string.IsNullOrWhiteSpace(argsPart) ?
                    Array.Empty<string>() : argsPart.Split(',');
                for (int i = 0; i < args.Length; i++)
                    args[i] = args[i].Trim();
                if (Enum.TryParse(func, out FunctionFloat ff))
                    return controller.GameLogic.EvaluateFunctionFloat(ff, args);
                return controller.GameLogic.EvaluateFunctionFloat(func, args);
            }
            return parsed.Interval;
        }
    }
}