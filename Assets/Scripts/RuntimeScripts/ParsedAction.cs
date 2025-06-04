using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Represents a single parsed line of a script.
    /// </summary>
    public class ParsedAction
    {
        public ActionType ActionType { get; set; }
        public List<string> Args { get; } = new List<string>();
        public float Interval { get; set; }
        public float Period { get; set; }
        public string Condition { get; set; }
        public string CanExecuteRaw { get; set; }
        public string IntervalFuncRaw { get; set; }
    }

    /// <summary>
    /// Enumeration of built-in action types.
    /// </summary>
    public enum ActionType
    {
        Attack,
        AddPlayerEffect,
        AddPlayerEffectFor,
        RemoveRandomDebuffPlayerEffect,
        AddMaxHp,
        SetNanikaEffectFor,
        SpawnNanika
    }
}