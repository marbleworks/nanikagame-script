using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Loads script files from the Resources folder and triggers events.
    /// </summary>
    public class RuntimeTextScriptController : MonoBehaviour
    {
        private readonly Dictionary<string, ParsedEvent> _events = new();
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
        /// Loads all script files from a Resources subfolder and merges their events.
        /// </summary>
        /// <param name="folder">The subfolder under Resources containing TextAsset scripts.</param>
        /// <param name="mode">How to merge the loaded events with existing ones.</param>
        public void Load(string folder, ScriptLoadMode mode = ScriptLoadMode.FullReplace)
        {
            var assets = Resources.LoadAll<TextAsset>(folder);
            var loadedEvents = new Dictionary<string, ParsedEvent>();

            foreach (var asset in assets)
            {
                var parsed = TextScriptParser.ParseString(asset.text);
                foreach (var kvp in parsed)
                {
                    if (loadedEvents.TryGetValue(kvp.Key, out var existing))
                        existing.Actions.AddRange(kvp.Value.Actions);
                    else
                        loadedEvents.Add(kvp.Key, kvp.Value);
                }
            }

            MergeEvents(loadedEvents, mode);
        }

        /// <summary>
        /// Loads a single script file from Resources and merges its events.
        /// </summary>
        /// <param name="path">Resource path of the script (without file extension).</param>
        /// <param name="mode">How to merge the loaded events with existing ones.</param>
        public void LoadFile(string path, ScriptLoadMode mode = ScriptLoadMode.FullReplace)
        {
            var resourcePath = path.EndsWith(".txt") ? path[..^4] : path;
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) return;

            var parsed = TextScriptParser.ParseString(asset.text);
            MergeEvents(parsed, mode);
        }

        /// <summary>
        /// Loads script events from a raw DSL string and merges them.
        /// </summary>
        /// <param name="script">The DSL script content as a string.</param>
        /// <param name="mode">How to merge the loaded events with existing ones.</param>
        public void LoadFromString(string script, ScriptLoadMode mode = ScriptLoadMode.FullReplace)
        {
            var parsed = TextScriptParser.ParseString(script);
            MergeEvents(parsed, mode);
        }

        /// <summary>
        /// Triggers the specified event, executing or scheduling its actions.
        /// </summary>
        /// <param name="eventName">The name of the event to trigger.</param>
        public void Trigger(string eventName)
        {
            if (!_events.TryGetValue(eventName, out var parsedEvent)) return;

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

        /// <summary>
        /// Merges the given events into the internal event dictionary.
        /// </summary>
        /// <param name="loaded">A dictionary of event names to ParsedEvent objects.</param>
        /// <param name="mode">Merge behavior for existing events.</param>
        private void MergeEvents(Dictionary<string, ParsedEvent> loaded, ScriptLoadMode mode)
        {
            if (mode == ScriptLoadMode.FullReplace)
            {
                _events.Clear();
            }

            foreach (var kvp in loaded)
            {
                if (_events.TryGetValue(kvp.Key, out var existing))
                {
                    switch (mode)
                    {
                        case ScriptLoadMode.Overwrite:
                            _events[kvp.Key] = kvp.Value;
                            break;
                        case ScriptLoadMode.Append:
                            existing.Actions.AddRange(kvp.Value.Actions);
                            break;
                        case ScriptLoadMode.FullReplace:
                            // _events was cleared, so just add
                            _events.Add(kvp.Key, kvp.Value);
                            break;
                    }
                }
                else
                {
                    _events.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
