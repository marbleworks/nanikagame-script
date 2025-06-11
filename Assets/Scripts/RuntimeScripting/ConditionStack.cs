using System.Collections.Generic;
using System.Text;

namespace RuntimeScripting
{
    /// <summary>
    /// Manages nested condition expressions during parsing.
    /// </summary>
    internal sealed class ConditionStack
    {
        private readonly Stack<string> _stack = new();

        public int Count => _stack.Count;

        public void Push(string condition) => _stack.Push(condition);

        public void Pop()
        {
            if (_stack.Count > 0)
                _stack.Pop();
        }

        /// <summary>
        /// Applies the combined condition to the given action.
        /// </summary>
        public void Apply(ParsedAction action)
        {
            if (_stack.Count == 0)
                return;
            var sb = new StringBuilder();
            foreach (var cond in _stack)
            {
                if (sb.Length > 0)
                    sb.Insert(0, "(").Append(") && ");
                sb.Insert(0, cond);
            }

            action.Condition = sb.ToString();
        }
    }
}
