using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Holds a list of actions associated with an event.
    /// </summary>
    public class ParsedEvent
    {
        public string EventName { get; set; }
        public List<ParsedAction> Actions { get; } = new List<ParsedAction>();
    }
}