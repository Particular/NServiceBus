namespace NServiceBus
{
    using System.Collections.Generic;
    using DeliveryConstraints;
    using Extensibility;
    using Pipeline;

    class OutgoingSendContext : OutgoingContext, IOutgoingSendContext
    {
        public OutgoingSendContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ContextBag extensions, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(extensions), extensions);

            Message = message;

            Merge(extensions);
            Set(ExtendableOptions.OperationPropertiesKey, extensions);
            Set(new List<DeliveryConstraint>(0)); // set empty delivery constraint to prevent leaking nested constraints collections.
        }

        public OutgoingLogicalMessage Message { get; }
    }
}