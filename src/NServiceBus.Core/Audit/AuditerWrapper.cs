namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Unicast;

    class AuditerWrapper : IAuditMessages
    {
        readonly IBuilder builder;

        public Type AuditerImplType { get; set; }

        public AuditerWrapper(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Audit(SendOptions sendOptions, OutgoingMessage message)
        {
            ((dynamic)builder.Build(AuditerImplType)).Audit(sendOptions, message);
        }
    }
}