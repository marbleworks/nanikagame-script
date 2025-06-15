using System.Collections.Generic;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Loads DSL scripts from Resources or raw strings and stores their events.
    /// </summary>
    public class ScriptLoader
    {
        private ScriptController _controller;
        private readonly Dictionary<string, ParsedEvent> _events = new();

        /// <summary>
        /// Creates a new loader with an optional script controller reference.
        /// </summary>
        /// <param name="controller">Controller used to execute events.</param>
        public ScriptLoader(ScriptController controller = null)
            => _controller = controller;

        /// <summary>
        /// Gets or sets the controller used for execution.
        /// </summary>
        public ScriptController Controller
        {
            get => _controller;
            set => _controller = value;
        }

        /// <summary>
        /// Registers the script controller after construction.
        /// </summary>
        /// <param name="controller">Controller used to execute events.</param>
        public void Initialize(ScriptController controller)
            => _controller = controller;

        /// <summary>
        /// Gets the loaded events by name.
        /// </summary>
        public IReadOnlyDictionary<string, ParsedEvent> Events => _events;

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
                if (asset == null || string.IsNullOrWhiteSpace(asset.text)) continue;
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

            var script = asset.text;
            if (string.IsNullOrWhiteSpace(script)) return;

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
            if (string.IsNullOrWhiteSpace(script)) return;

            var parsed = TextScriptParser.ParseString(script);
            MergeEvents(parsed, mode);
        }

        /// <summary>
        /// Attempts to get a parsed event by name.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="parsedEvent">The parsed event if found.</param>
        /// <returns>True if the event exists.</returns>
        public bool TryGetEvent(string eventName, out ParsedEvent parsedEvent)
            => _events.TryGetValue(eventName, out parsedEvent);

        /// <summary>
        /// Triggers the specified event on the registered controller.
        /// </summary>
        /// <param name="eventName">Name of the event to trigger.</param>
        public void Trigger(string eventName)
        {
            if (_controller == null) return;
            if (_events.TryGetValue(eventName, out var parsedEvent))
                _controller.Trigger(parsedEvent);
        }

        /// <summary>
        /// Invokes <see cref="ScriptController.ExecuteString"/> on the registered controller.
        /// </summary>
        /// <param name="script">DSL text containing action statements.</param>
        public void ExecuteString(string script)
            => _controller?.ExecuteString(script);

        /// <summary>
        /// Invokes <see cref="ScriptController.ExecuteEasyScript"/> on the registered controller.
        /// </summary>
        /// <param name="easyScript">Short-form script to execute.</param>
        public void ExecuteEasyScript(string easyScript)
            => _controller?.ExecuteEasyScript(easyScript);

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
