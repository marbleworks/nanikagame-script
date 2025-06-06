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
        /// Loads all script files from a Resources folder.
        /// </summary>
        /// <param name="folder">Resources subfolder containing the scripts.</param>
        public void Load(string folder)
        {
            var loaded = TextScriptParser.LoadScripts(folder);
            MergeEvents(loaded);
        }

        /// <summary>
        /// Loads all script files written in the new DSL from a Resources folder.
        /// </summary>
        /// <param name="folder">Resources subfolder containing the scripts.</param>
        public void LoadV2(string folder)
        {
            var loaded = new Dictionary<string, ParsedEvent>();
            var assets = Resources.LoadAll<TextAsset>(folder);
            foreach (var asset in assets)
            {
                var parsed = NewTextScriptParser.ParseString(asset.text);
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
        /// Loads a single script file from the Resources folder.
        /// </summary>
        /// <param name="path">Resource path of the script without extension.</param>
        public void LoadFile(string path)
        {
            var loaded = TextScriptParser.LoadFile(path);
            MergeEvents(loaded);
        }

        /// <summary>
        /// Loads a single script file written in the new DSL from the Resources folder.
        /// </summary>
        /// <param name="path">Resource path of the script without extension.</param>
        public void LoadFileV2(string path)
        {
            if (path.EndsWith(".txt"))
                path = path.Substring(0, path.Length - 4);
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
                return;
            var parsed = NewTextScriptParser.ParseString(asset.text);
            MergeEvents(parsed);
        }

        /// <summary>
        /// Loads script events from a string.
        /// </summary>
        /// <param name="script">Script contents in the DSL format.</param>
        public void LoadFromString(string script)
        {
            var loaded = TextScriptParser.ParseString(script);
            MergeEvents(loaded);
        }

        /// <summary>
        /// Loads events from a string written in the new block-style DSL.
        /// </summary>
        /// <param name="script">Script contents in the new DSL format.</param>
        public void LoadFromStringV2(string script)
        {
            var loaded = NewTextScriptParser.ParseString(script);
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
                    var param = CreateParameter(pa);
                    ExecuteActionImmediately(param);
                }
            }
        }

        private IEnumerator RunScheduledAction(ScheduledAction sa)
        {
            yield return sa.ExecuteCoroutine();
            scheduled.Remove(sa);
        }

        internal void ExecuteActionImmediately(ActionParameter param)
        {
            switch (param.ActionType)
            {
                case ActionType.Attack:
                    GameLogic.Attack(param.IntValue);
                    break;
                case ActionType.AddPlayerEffect:
                    GameLogic.AddPlayerEffect(param.Targets, param.StringValue, param.IntValue);
                    break;
                case ActionType.AddPlayerEffectFor:
                    GameLogic.AddPlayerEffectFor(param.Targets, param.StringValue, param.IntValue, param.ExtraValue);
                    break;
                case ActionType.RemoveRandomDebuffPlayerEffect:
                    GameLogic.RemoveRandomDebuffPlayerEffect(param.Targets, param.IntValue);
                    break;
                case ActionType.AddMaxHp:
                    GameLogic.AddMaxHp(param.Targets, param.IntValue);
                    break;
                case ActionType.SetNanikaEffectFor:
                    GameLogic.SetNanikaEffectFor(param.Targets, param.StringValue, param.IntValue);
                    break;
                case ActionType.SpawnNanika:
                    GameLogic.SpawnNanika(param.Targets, param.StringValue, param.IntValue);
                    break;
            }
        }

        internal ActionParameter CreateParameter(ParsedAction pa)
        {
            var param = new ActionParameter
            {
                ActionType = pa.ActionType
            };

            switch (pa.ActionType)
            {
                case ActionType.Attack:
                    if (pa.Args.Count > 0)
                        param.IntValue = ParseIntArg(pa.Args[0]);
                    break;
                case ActionType.AddPlayerEffect:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.StringValue = pa.Args[1];
                    if (pa.Args.Count > 2)
                        param.IntValue = ParseIntArg(pa.Args[2]);
                    break;
                case ActionType.AddPlayerEffectFor:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.StringValue = pa.Args[1];
                    if (pa.Args.Count > 2)
                        param.IntValue = ParseIntArg(pa.Args[2]);
                    if (pa.Args.Count > 3)
                        param.ExtraValue = ParseIntArg(pa.Args[3]);
                    break;
                case ActionType.RemoveRandomDebuffPlayerEffect:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.IntValue = ParseIntArg(pa.Args[1]);
                    break;
                case ActionType.AddMaxHp:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.IntValue = ParseIntArg(pa.Args[1]);
                    break;
                case ActionType.SetNanikaEffectFor:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.StringValue = pa.Args[1];
                    if (pa.Args.Count > 2)
                        param.IntValue = ParseIntArg(pa.Args[2]);
                    break;
                case ActionType.SpawnNanika:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.StringValue = pa.Args[1];
                    if (pa.Args.Count > 2)
                        param.IntValue = ParseIntArg(pa.Args[2]);
                    break;
                default:
                    if (pa.Args.Count > 0)
                        param.Targets = pa.Args[0];
                    if (pa.Args.Count > 1)
                        param.StringValue = pa.Args[1];
                    if (pa.Args.Count > 2)
                        param.IntValue = ParseIntArg(pa.Args[2]);
                    if (pa.Args.Count > 3)
                        param.ExtraValue = ParseIntArg(pa.Args[3]);
                    break;
            }

            return param;
        }

        private int ParseIntArg(string arg)
        {
            if (int.TryParse(arg, out var value))
                return value;

            return IntExpressionEvaluator.Evaluate(arg, GameLogic);
        }
    }
}
