// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingDistributionContext" />.
    /// </summary>
    public class TestableOutgoingDistributionContext : TestableOutgoingContext, IOutgoingDistributionContext
    {
        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());

        /// <summary>
        /// The collection of logical endpoint names to which a message should be delivered.
        /// </summary>
        public IReadOnlyCollection<UnicastRoute> Destinations { get; set; } = new UnicastRoute[0];

        /// <summary>
        /// The intent of the message.
        /// </summary>
        public DistributionStrategyScope DistributionScope { get; set; } = DistributionStrategyScope.Send;
        
    }
}