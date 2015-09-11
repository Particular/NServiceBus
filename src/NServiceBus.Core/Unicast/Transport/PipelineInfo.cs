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
        public PipelineInfo(string name, string transportAddress)
        {
            Name = name;
            TransportAddress = transportAddress;
        }

        /// <summary>
        /// Id/name of the pipeline.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The transport address of the receiver bound to this pipeline.
        /// </summary>
        public string TransportAddress { get; private set; }
    }
}