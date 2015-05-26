namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// Contains information about pipeline.
    /// </summary>
    public class PipelineInfo
    {
        readonly string name;
        readonly string publicAddress;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="publicAddress"></param>
        public PipelineInfo(string name, string publicAddress)
        {
            this.name = name;
            this.publicAddress = publicAddress;
        }

        /// <summary>
        /// Id/name of the pipeline
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Public address to which messages should be sent to get to this pipeline's input queue. Might differe from the input queue.
        /// </summary>
        public string PublicAddress
        {
            get { return publicAddress; }
        }
    }
}