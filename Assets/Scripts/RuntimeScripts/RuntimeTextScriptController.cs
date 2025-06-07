using System;
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
        private readonly Dictionary<string, ParsedEvent> events = new Dictionary<string, ParsedEvent>();
        private readonly List<ScheduledAction> scheduled = new List<ScheduledAction>();
        private readonly List<Coroutine> running = new List<Coroutine>();

        internal GameLogic GameLogic;

        public void SetGameLogic(GameLogic gameLogic)
        {
            GameLogic = gameLogic;
        }

        /// <summary>
        /// Loads all script files written in the new DSL from a Resources folder.
        /// </summary>
        /// <param name="folder">Resources subfolder containing the scripts.</param>
        public void Load(string folder)
        {
            var loaded = new Dictionary<string, ParsedEvent>();
            var assets = Resources.LoadAll<TextAsset>(folder);
            foreach (var asset in assets)
            {
                var parsed = TextScriptParser.ParseString(asset.text);
                foreach (var kv in parsed)
                {
                    if (!loaded.ContainsKey(kv.Key))
                        loaded[kv.Key] = kv.Value;
                    else
                        loaded[kv.Key].Actions.AddRange(kv.Value.Actions);
                }
            }
            MergeEvents(loaded);
        }

        /// <summary>
        /// Loads a single script file written in the new DSL from the Resources folder.
        /// </summary>
        /// <param name="path">Resource path of the script without extension.</param>
        public void LoadFile(string path)
        {
            if (path.EndsWith(".txt"))
                path = path.Substring(0, path.Length - 4);
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
                return;
            var parsed = TextScriptParser.ParseString(asset.text);
            MergeEvents(parsed);
        }

        /// <summary>
        /// Loads events from a string written in the new block-style DSL.
        /// </summary>
        /// <param name="script">Script contents in the new DSL format.</param>
        public void LoadFromString(string script)
        {
            var loaded = TextScriptParser.ParseString(script);
            MergeEvents(loaded);
        }

        private void MergeEvents(Dictionary<string, ParsedEvent> loaded)
        {
            events.Clear();
            foreach (var kv in loaded)
            {
                if (!events.ContainsKey(kv.Key))
                    events[kv.Key] = kv.Value;
                else
                    events[kv.Key].Actions.AddRange(kv.Value.Actions);
            }
        }

        public void Trigger(string eventName)
        {
            if (!events.TryGetValue(eventName, out var pe))
                return;

            foreach (var pa in pe.Actions)
            {
                if (!string.IsNullOrEmpty(pa.Condition) && !ConditionEvaluator.Evaluate(pa.Condition, GameLogic))
                    continue;

                if (pa.Interval > 0 || !string.IsNullOrEmpty(pa.IntervalFuncRaw))
                {
                    var sa = new ScheduledAction(pa, this);
                    scheduled.Add(sa);
                    var co = StartCoroutine(RunScheduledAction(sa));
                    running.Add(co);
                }
                else
                {
                    var param = GameLogic.CreateParameter(pa);
                    GameLogic.ExecuteAction(param);
                }
            }
        }

        private IEnumerator RunScheduledAction(ScheduledAction sa)
        {
            yield return sa.ExecuteCoroutine();
            scheduled.Remove(sa);
        }

        
    }
}
