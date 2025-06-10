using System;
using System.Collections;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Handles periodic execution of actions with configurable intervals, periods, and conditions,
    /// ensuring no cumulative drift due to frame time or WaitForSeconds inaccuracies.
    /// </summary>
    public class ScheduledAction
    {
        private readonly ParsedAction _parsed;
        private readonly RuntimeTextScriptController _controller;
        private readonly float _period;
        private float _interval;
        private float _elapsed;
        private int _executedCount;

        public ScheduledAction(ParsedAction parsed, RuntimeTextScriptController controller)
        {
            _parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _period = GetEvaluatedValue(parsed.Period, parsed.PeriodFuncRaw);
            _interval = GetEvaluatedValue(parsed.Interval, parsed.IntervalFuncRaw);
        }

        /// <summary>
        /// Coroutine that executes the parsed action based on interval, period, max count, and optional conditions.
        /// Uses absolute timing to prevent cumulative drift.
        /// </summary>
        public IEnumerator ExecuteCoroutine()
        {
            var startTime = Time.time;
            var nextExecution = startTime + _interval;

            while (CanContinue())
            {
                // Calculate time until next execution to avoid drift
                var waitTime = nextExecution - Time.time;
                yield return waitTime > 0f ? new WaitForSeconds(waitTime) : null;

                // Update elapsed using absolute time
                _elapsed = Time.time - startTime;

                // Check "while" condition before execution
                if (!EvaluateCondition(_parsed.WhileRaw))
                    yield break;

                // Perform action if allowed
                if (EvaluateCondition(_parsed.CanExecuteRaw))
                {
                    _controller.GameLogic.ExecuteAction(_parsed);
                    _executedCount++;
                }

                // Re-evaluate interval if dynamic
                _interval = GetEvaluatedValue(_parsed.Interval, _parsed.IntervalFuncRaw);
                nextExecution += _interval;
            }
        }

        /// <summary>
        /// Determines if the action should continue based on elapsed time, period, and execution count.
        /// </summary>
        private bool CanContinue()
            => (_period <= 0f || _elapsed < _period)
               && (_parsed.MaxCount <= 0 || _executedCount < _parsed.MaxCount);

        /// <summary>
        /// Evaluates raw condition string, returns true if empty or evaluation passes.
        /// </summary>
        private bool EvaluateCondition(string conditionRaw)
            => string.IsNullOrEmpty(conditionRaw)
               || _controller.GameLogic.EvaluateCondition(conditionRaw);

        /// <summary>
        /// Evaluates a base value or an expression string to a float.
        /// </summary>
        private static float GetEvaluatedValue(float baseValue, string expressionRaw)
        {
            if (string.IsNullOrEmpty(expressionRaw))
                return baseValue;

            try
            {
                return IntExpressionEvaluator.EvaluateFloat(expressionRaw, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to evaluate expression '{expressionRaw}': {ex.Message}");
                return baseValue;
            }
        }
    }
}
