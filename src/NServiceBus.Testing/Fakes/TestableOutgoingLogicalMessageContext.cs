namespace NServiceBus.Testing.Fakes
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    public class TestableOutgoingLogicalMessageContext : OutgoingLogicalMessageContext
    {
        public TestableOutgoingLogicalMessageContext(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext parentContext) : base(messageId, headers, message, routingStrategies, parentContext)
        {
        }
    }
}