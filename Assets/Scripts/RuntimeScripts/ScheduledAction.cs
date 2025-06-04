using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Handles periodic execution of actions.
    /// </summary>
    public class ScheduledAction
    {
        private readonly ActionParameter param;
        private readonly ParsedAction parsed;
        private readonly RuntimeTextScriptController controller;
        private float elapsed;
        private float nextTime;

        public ScheduledAction(ActionParameter param, ParsedAction parsed, RuntimeTextScriptController controller)
        {
            this.param = param;
            this.parsed = parsed;
            this.controller = controller;
            nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
        }

        /// <summary>
        /// Updates the internal timer and executes if interval elapsed.
        /// </summary>
        /// <param name="delta">Delta time in seconds.</param>
        /// <returns>True if still active.</returns>
        public bool Update(float delta)
        {
            elapsed += delta;
            if (parsed.Period > 0 && elapsed > parsed.Period)
            {
                return false;
            }

            if (elapsed >= nextTime)
            {
                if (string.IsNullOrEmpty(parsed.CanExecuteRaw) || ConditionEvaluator.Evaluate(parsed.CanExecuteRaw))
                {
                    controller.ExecuteActionImmediately(param);
                }
                nextTime = elapsed + (parsed.Interval > 0 ? parsed.Interval : EvaluateInterval());
            }
            return true;
        }

        private float EvaluateInterval()
        {
            if (string.IsNullOrEmpty(parsed.IntervalFuncRaw))
                return parsed.Interval;
            // very simple pattern: interval(x)
            var s = parsed.IntervalFuncRaw.Trim();
            if (s.StartsWith("interval(") && s.EndsWith(")"))
            {
                var inner = s.Substring(9, s.Length - 10);
                if (float.TryParse(inner, out float val))
                    return val;
            }
            return parsed.Interval;
        }
    }
}