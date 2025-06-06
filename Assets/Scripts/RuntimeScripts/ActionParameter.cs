namespace RuntimeScripting
{
    /// <summary>
    /// Parameter container used when executing actions.
    /// </summary>
    public class ActionParameter
    {
        public ActionType ActionType;
        public string FunctionName;
        public System.Collections.Generic.List<string> Args = new System.Collections.Generic.List<string>();
        public string Targets;
        public string StringValue;
        public int IntValue;
        public int ExtraValue;
    }
}