using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Loads script files and triggers events.
    /// </summary>
    public class RuntimeTextScriptController
    {
        private readonly Dictionary<string, ParsedEvent> events = new Dictionary<string, ParsedEvent>();
        private readonly List<ScheduledAction> scheduled = new List<ScheduledAction>();

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

        public void Trigger(string eventName)
        {
            if (!events.TryGetValue(eventName, out var pe))
                return;

            foreach (var pa in pe.Actions)
            {
                if (!string.IsNullOrEmpty(pa.Condition) && !ConditionEvaluator.Evaluate(pa.Condition))
                    continue;

                var param = Convert(pa);
                if (pa.Interval > 0 || !string.IsNullOrEmpty(pa.IntervalFuncRaw))
                {
                    scheduled.Add(new ScheduledAction(param, pa, this));
                }
                else
                {
                    ExecuteActionImmediately(param);
                }
            }
        }

        public void Update(float delta)
        {
            for (int i = scheduled.Count - 1; i >= 0; i--)
            {
                if (!scheduled[i].Update(delta))
                    scheduled.RemoveAt(i);
            }
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