namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Represents a running node.
    /// </summary>
    public class RunningNode : IEndpointInstance
    {
        internal RunningNode(IBuilder builder, PipelineCollection pipelineCollection)
        {
            this.pipelineCollection = pipelineCollection;
            this.builder = builder;
        }

        /// <summary>
        /// Stops the node.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            if (stopped)
            {
                throw new InvalidOperationException("Endpoint already stopped.");
            }

            Log.Info("Initiating shutdown.");

            await pipelineCollection.Stop().ConfigureAwait(false);
            builder.Dispose();
            stopped = true;
            Log.Info("Shutdown complete.");
        }
        
        volatile bool stopped;
        PipelineCollection pipelineCollection;
        IBuilder builder;

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        /// <summary>
        /// Not implemented in node.
        /// </summary>
        public IBusContext CreateBusContext()
        {
            throw new NotImplementedException("Node does not allow to create bus contexts.");
        }
    }
}