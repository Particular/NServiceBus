namespace NServiceBus.Pipeline
{
    /// <summary>
    /// Pipe execution started.
    /// </summary>
    public struct PipeStarted
    {
        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get; set; }
    }
}