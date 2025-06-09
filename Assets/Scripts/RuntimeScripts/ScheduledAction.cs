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
        private float periodLimit;
        private int executedCount;

        public ScheduledAction(ParsedAction parsed, RuntimeTextScriptController controller)
        {
            this.parsed = parsed;
            this.controller = controller;
            periodLimit = parsed.Period > 0 ? parsed.Period : EvaluatePeriod();
            nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
        }

        /// <summary>
        /// Coroutine execution handling intervals automatically.
        /// </summary>
        public IEnumerator ExecuteCoroutine()
        {
            elapsed = 0f;
            nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
            var start = Time.time;
            var nextExecution = start + nextTime;

            while (BasicContinue())
            {
                var wait = Mathf.Max(0f, nextExecution - Time.time);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
                else
                    yield return null;

                elapsed = Time.time - start;

                if (!ShouldContinueWithWhile())
                    yield break;

                if (string.IsNullOrEmpty(parsed.CanExecuteRaw) ||
                    ConditionEvaluator.Evaluate(parsed.CanExecuteRaw, controller.GameLogic))
                {
                    controller.GameLogic.ExecuteAction(parsed);
                    executedCount++;
                }

                nextTime = parsed.Interval > 0 ? parsed.Interval : EvaluateInterval();
                nextExecution += nextTime;
            }
        }

        /// <summary>
        /// Determines whether execution should continue ignoring the 'while' expression.
        /// </summary>
        /// <returns>True to keep running; false to stop.</returns>
        private bool BasicContinue()
        {
            if (periodLimit > 0 && elapsed >= periodLimit)
                return false;
            if (parsed.MaxCount > 0 && executedCount >= parsed.MaxCount)
                return false;
            return true;
        }

        /// <summary>
        /// Determines whether execution should continue, including the 'while' expression.
        /// </summary>
        /// <returns>True to keep running; false to stop.</returns>
        private bool ShouldContinueWithWhile()
        {
            if (!BasicContinue())
                return false;
            if (!string.IsNullOrEmpty(parsed.WhileRaw) &&
                !ConditionEvaluator.Evaluate(parsed.WhileRaw, controller.GameLogic))
                return false;
            return true;
        }

        private float EvaluateInterval()
        {
            if (string.IsNullOrEmpty(parsed.IntervalFuncRaw))
                return parsed.Interval;

            return IntExpressionEvaluator.EvaluateFloat(parsed.IntervalFuncRaw, controller.GameLogic);
        }

        private float EvaluatePeriod()
        {
            if (string.IsNullOrEmpty(parsed.PeriodFuncRaw))
                return parsed.Period;

            return IntExpressionEvaluator.EvaluateFloat(parsed.PeriodFuncRaw, controller.GameLogic);
        }
    }
}
