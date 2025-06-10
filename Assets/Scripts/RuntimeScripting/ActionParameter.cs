namespace RuntimeScripting
{
    /// <summary>
    /// Parameter container used when executing actions.
    /// </summary>
    public class ActionParameter
    {
        public string FunctionName;
        public System.Collections.Generic.List<string> Args = new();
    }
}