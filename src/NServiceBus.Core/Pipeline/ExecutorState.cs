namespace NServiceBus.Pipeline
{
    /// <summary>
    /// Used by <see cref="ExecutorNotifications"/> to notify about the state of executors.
    /// </summary>
    public struct ExecutorState
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExecutorState"/>.
        /// </summary>
        public ExecutorState(string[] pipelineIds, int currentConcurrencyLevel) : this()
        {
            PipelineIds = pipelineIds;
            CurrentConcurrencyLevel = currentConcurrencyLevel;
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] PipelineIds { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int CurrentConcurrencyLevel { get; private set; }
    }
}