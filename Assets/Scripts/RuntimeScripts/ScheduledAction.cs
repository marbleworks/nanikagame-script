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
            while (periodLimit <= 0 || elapsed < periodLimit)
            {
                yield return new WaitForSeconds(nextTime);
                elapsed += nextTime;
                if (periodLimit > 0 && elapsed > periodLimit)
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