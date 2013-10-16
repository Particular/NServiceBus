namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using Saga;

    public class SagaWithUniqueProperty : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }

        public string NonUniqueString { get; set; }
    }
}