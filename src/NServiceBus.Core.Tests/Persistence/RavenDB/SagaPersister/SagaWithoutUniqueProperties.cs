namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NServiceBus.Saga;

    public class SagaWithoutUniqueProperties : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string UniqueString { get; set; }

        public string NonUniqueString { get; set; }
    }
}