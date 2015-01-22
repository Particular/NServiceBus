namespace NServiceBus.Pipeline
{
    /// <summary>
    /// 
    /// </summary>
    public struct ExecutorState
    {
        readonly string[] pipelineIds;
        readonly int currentConcurrencyLevel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipelineIds"></param>
        /// <param name="currentConcurrencyLevel"></param>
        public ExecutorState(string[] pipelineIds, int currentConcurrencyLevel) : this()
        {
            this.pipelineIds = pipelineIds;
            this.currentConcurrencyLevel = currentConcurrencyLevel;
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] PipelineIds
        {
            get { return pipelineIds; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int CurrentConcurrencyLevel
        {
            get { return currentConcurrencyLevel; }
        }
    }
}