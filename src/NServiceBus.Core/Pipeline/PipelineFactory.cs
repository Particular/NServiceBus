namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// </summary>
    public interface PipelineFactory
    {
        /// <summary>
        /// Internal
        /// </summary>
        /// <returns></returns>
        IEnumerable<TransportReceiver> BuildPipelines(IBuilder builder, ReadOnlySettings settings, IExecutor executor);
    }
}