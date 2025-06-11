namespace RuntimeScripting
{
    /// <summary>
    /// Determines how newly loaded script events are merged with existing ones.
    /// </summary>
    public enum ScriptLoadMode
    {
        /// <summary>
        /// Clear all existing events and replace them entirely with the new set.
        /// </summary>
        FullReplace,

        /// <summary>
        /// Replace only events that share the same name, keeping others intact.
        /// </summary>
        Overwrite,

        /// <summary>
        /// Append actions from new events to existing ones if present, otherwise add.
        /// </summary>
        Append
    }
}
