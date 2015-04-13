namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.ObjectBuilder;

    class AuditerWrapper : IAuditMessages
    {
        readonly IBuilder builder;

        public Type AuditerImplType { get; set; }

        public AuditerWrapper(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Audit(OutgoingMessage message,TransportSendOptions sendOptions)
        {
            ((dynamic)builder.Build(AuditerImplType)).Audit(sendOptions, message);
        }
    }
}