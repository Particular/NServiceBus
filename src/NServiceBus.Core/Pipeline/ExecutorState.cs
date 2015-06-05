namespace NServiceBus.Pipeline
{
    /// <summary>
    /// 
    /// </summary>
    public struct ExecutorState
    {
        /// <summary>
        /// 
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