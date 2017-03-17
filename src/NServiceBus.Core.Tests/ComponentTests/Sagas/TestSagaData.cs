namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
        {
            mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
        }
    }

    class StartMessage
    {
        public string SomeId { get; set; }
    }

    public class TestSagaData : ContainSagaData
    {
        public string SomeId { get; set; } = "Test";

        public RelatedClass RelatedClass { get; set; }

        public IList<OrderLine> OrderLines { get; set; }

        public StatusEnum Status { get; set; }

        public DateTime DateTimeProperty { get; set; } = DateTime.UtcNow;

        public TestComponent TestComponent { get; set; }

        public PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }
    }

    public class PolymorphicProperty : PolymorphicPropertyBase
    {
        public int SomeInt { get; set; }
    }

    public class PolymorphicPropertyBase
    {
        public Guid Id { get; set; }
    }

    public enum StatusEnum
    {
        SomeStatus,
        AnotherStatus
    }

    public class TestComponent
    {
        public string Property { get; set; }
        public string AnotherProperty { get; set; }
    }

    public class OrderLine
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
    }

    public class RelatedClass
    {
        public Guid Id { get; set; }

        public TestSagaData ParentSaga { get; set; }
    }
}