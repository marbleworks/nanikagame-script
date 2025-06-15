using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Executes parsed script events and manages scheduled actions.
    /// </summary>
    public class ScriptController : MonoBehaviour
    {
        private readonly List<ScheduledAction> _scheduled = new();
        private readonly List<Coroutine> _running = new();

        /// <summary>
        /// Gets the reference to the GameLogic instance.
        /// </summary>
        public IGameLogic GameLogic { get; private set; }

        /// <summary>
        /// Initializes the controller with the specified GameLogic.
        /// </summary>
        /// <param name="gameLogic">The GameLogic instance to use.</param>
        public void Initialize(IGameLogic gameLogic) => GameLogic = gameLogic;


        /// <summary>
        /// Executes the actions contained in the given parsed event.
        /// </summary>
        /// <param name="parsedEvent">The event to execute.</param>
        public void Trigger(ParsedEvent parsedEvent)
        {
            if (parsedEvent == null) return;

            ExecuteActions(parsedEvent.Actions);
        }

        /// <summary>
        /// Executes DSL text that contains only action statements without an event block.
        /// </summary>
        /// <param name="script">The raw DSL text.</param>
        public void ExecuteString(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            const string tempEvent = "OnImmediate";
            var wrapped = $"[{tempEvent}]\n" + script;
            var parsed = TextScriptParser.ParseString(wrapped);
            if (parsed.TryGetValue(tempEvent, out var evt))
            {
                ExecuteActions(evt.Actions);
            }
        }

        public void ExecuteEasyScript(string easyScript)
        {
            ExecuteString(FormatAction(easyScript));
        }

        private static string FormatAction(string input)
        {
            var parts = input.Split(':');

            var actBody  = parts.Length > 0 ? parts[0] : string.Empty;
            var interval = parts.Length > 1 ? parts[1] : string.Empty;
            var period   = parts.Length > 2 ? parts[2] : string.Empty;
            var maxCount = parts.Length > 3 ? parts[3] : string.Empty;

            var sb = new StringBuilder();
            sb.Append($"act {{ {actBody} }} mod {{ ");

            if (!string.IsNullOrEmpty(interval))
                sb.Append($"interval = {interval}, ");
            if (!string.IsNullOrEmpty(period))
                sb.Append($"period = {period}, ");
            if (!string.IsNullOrEmpty(maxCount))
                sb.Append($"maxCount = {maxCount}");

            sb.Append(" };");

            return sb.ToString();
        }

        private void ExecuteActions(List<ParsedAction> actions)
        {
            foreach (var action in actions)
            {
                if (!string.IsNullOrEmpty(action.Condition) &&
                    !GameLogic.EvaluateCondition(action.Condition))
                {
                    continue;
                }

                if (action.Interval > 0 || !string.IsNullOrEmpty(action.IntervalFuncRaw))
                {
                    var scheduled = new ScheduledAction(action, this);
                    _scheduled.Add(scheduled);
                    _running.Add(StartCoroutine(RunScheduledAction(scheduled)));
                }
                else
                {
                    GameLogic.ExecuteAction(action);
                }
            }
        }

        private IEnumerator RunScheduledAction(ScheduledAction scheduled)
        {
            yield return scheduled.ExecuteCoroutine();
            _scheduled.Remove(scheduled);
        }

    }
}
