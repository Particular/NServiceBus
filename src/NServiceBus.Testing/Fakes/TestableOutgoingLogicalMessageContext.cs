namespace NServiceBus.Testing.Fakes
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    public class TestableOutgoingLogicalMessageContext : TestableOutgoingContext, OutgoingLogicalMessageContext
    {
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(new object());
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; } = new List<RoutingStrategy>();

        public void UpdateMessageInstance(object newInstance)
        {
            UpdatedMessageInstances.Add(newInstance);
        }

        public List<object> UpdatedMessageInstances = new List<object>();
    }
}