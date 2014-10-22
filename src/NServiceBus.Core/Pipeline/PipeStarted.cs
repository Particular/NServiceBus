namespace NServiceBus.Pipeline
{
    /// <summary>
    /// Pipe execution started.
    /// </summary>
    public struct PipeStarted
    {
        readonly string pipeId;

        /// <summary>
        /// Creates an instance of <see cref="PipeStarted"/>.
        /// </summary>
        /// <param name="pipeId">Pipe identifier.</param>
        public PipeStarted(string pipeId)
        {
            this.pipeId = pipeId;
        }

        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get { return pipeId; } }
    }
}