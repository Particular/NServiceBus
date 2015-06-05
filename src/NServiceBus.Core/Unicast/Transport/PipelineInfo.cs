namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// Contains information about pipeline.
    /// </summary>
    public class PipelineInfo
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public PipelineInfo(string name, string publicAddress)
        {
            Name = name;
            PublicAddress = publicAddress;
        }

        /// <summary>
        /// Id/name of the pipeline
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Public address to which messages should be sent to get to this pipeline's input queue. Might differe from the input queue.
        /// </summary>
        public string PublicAddress { get; set; }
    }
}