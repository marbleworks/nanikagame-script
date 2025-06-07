namespace RuntimeScripting
{
    /// <summary>
    /// Parameter container used when executing actions.
    /// </summary>
    public class ActionParameter
    {
        public string FunctionName;
        public System.Collections.Generic.List<string> Args = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Result from executing an integer-returning function.
        /// </summary>
        public int? IntResult;

        /// <summary>
        /// Result from executing a float-returning function.
        /// </summary>
        public float? FloatResult;
    }
}