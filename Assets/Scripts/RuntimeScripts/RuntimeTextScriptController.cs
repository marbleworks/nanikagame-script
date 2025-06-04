using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Loads script files and triggers events.
    /// </summary>
    public class RuntimeTextScriptController : MonoBehaviour
    {
        private readonly Dictionary<string, ParsedEvent> events = new Dictionary<string, ParsedEvent>();
        private readonly List<ScheduledAction> scheduled = new List<ScheduledAction>();
        private readonly List<Coroutine> running = new List<Coroutine>();
        [SerializeField]
        private GameLogic gameLogic;

        public RuntimeTextScriptController(GameLogic gameLogic = null)
        {
            this.gameLogic = gameLogic ?? new GameLogic();
        }

        private void Awake()
        {
            if (gameLogic == null)
                gameLogic = new GameLogic();
        }

        internal GameLogic GameLogic => gameLogic;

        public void Load(string folder)
        {
            events.Clear();
            var loaded = TextScriptParser.LoadScripts(folder);
            foreach (var kv in loaded)
            {
                if (!events.ContainsKey(kv.Key))
                    events[kv.Key] = kv.Value;
                else
                    events[kv.Key].Actions.AddRange(kv.Value.Actions);
            }
        }

        /// <summary>
        /// Loads script events from a string.
        /// </summary>
        /// <param name="script">Script contents in the DSL format.</param>
        public void LoadFromString(string script)
        {
            events.Clear();
            var loaded = TextScriptParser.ParseString(script);
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
                if (!string.IsNullOrEmpty(pa.Condition) && !ConditionEvaluator.Evaluate(pa.Condition, gameLogic))
                    continue;

                var param = Convert(pa);
                if (pa.Interval > 0 || !string.IsNullOrEmpty(pa.IntervalFuncRaw))
                {
                    var sa = new ScheduledAction(param, pa, this);
                    scheduled.Add(sa);
                    var co = StartCoroutine(RunScheduledAction(sa));
                    running.Add(co);
                }
                else
                {
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
                    gameLogic.Attack(param.IntValue);
                    break;
                case ActionType.AddPlayerEffect:
                    gameLogic.AddPlayerEffect(param.Targets, param.StringValue, param.IntValue);
                    break;
                case ActionType.AddPlayerEffectFor:
                    gameLogic.AddPlayerEffectFor(param.Targets, param.StringValue, param.IntValue, param.ExtraValue);
                    break;
                case ActionType.RemoveRandomDebuffPlayerEffect:
                    gameLogic.RemoveRandomDebuffPlayerEffect(param.Targets, param.IntValue);
                    break;
                case ActionType.AddMaxHp:
                    gameLogic.AddMaxHp(param.Targets, param.IntValue);
                    break;
                case ActionType.SetNanikaEffectFor:
                    gameLogic.SetNanikaEffectFor(param.Targets, param.StringValue, param.IntValue);
                    break;
                case ActionType.SpawnNanika:
                    gameLogic.SpawnNanika(param.Targets, param.StringValue, param.IntValue);
                    break;
            }
        }

        private ActionParameter Convert(ParsedAction pa)
        {
            var param = new ActionParameter
            {
                ActionType = pa.ActionType
            };
            if (pa.Args.Count > 0)
                param.Targets = pa.Args[0];
            if (pa.Args.Count > 1)
                param.StringValue = pa.Args[1];
            if (pa.Args.Count > 2 && int.TryParse(pa.Args[2], out int iv))
                param.IntValue = iv;
            if (pa.Args.Count > 3 && int.TryParse(pa.Args[3], out int ex))
                param.ExtraValue = ex;
            if (pa.Interval > 0)
                param.IntValue = (int)pa.Interval;
            if (pa.Period > 0)
                param.ExtraValue = (int)pa.Period;
            return param;
        }
    }
}