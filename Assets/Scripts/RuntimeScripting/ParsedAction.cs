using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Represents a single parsed line of a script.
    /// </summary>
    public class ParsedAction
    {
        public string FunctionName { get; set; }
        public List<string> Args { get; } = new();
        public float Interval { get; set; }
        public float Period { get; set; }
        public string PeriodFuncRaw { get; set; }
        public string Condition { get; set; }
        public string CanExecuteRaw { get; set; }
        public string IntervalFuncRaw { get; set; }
        /// <summary>
        /// Maximum number of times the action will execute. Zero means unlimited.
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// Raw expression evaluated each cycle; execution stops when it becomes false.
        /// </summary>
        public string WhileRaw { get; set; }
    }
}
