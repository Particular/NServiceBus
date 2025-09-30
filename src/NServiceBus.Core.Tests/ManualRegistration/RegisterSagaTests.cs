namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using NUnit.Framework;

[TestFixture]
public class RegisterSagaTests
{
    [Test]
    public void RegisterSaga_Generic_Should_Store_Saga_Types()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterSaga<TestSaga, TestSagaData>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredSagas sagas), Is.True);
        Assert.That(sagas.Sagas, Has.Count.EqualTo(1));
        
        var registration = sagas.Sagas[0];
        Assert.That(registration.SagaType, Is.EqualTo(typeof(TestSaga)));
        Assert.That(registration.SagaDataType, Is.EqualTo(typeof(TestSagaData)));
    }

    [Test]
    public void RegisterSaga_NonGeneric_Should_Store_Saga_Types()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterSaga(typeof(TestSaga), typeof(TestSagaData));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredSagas sagas), Is.True);
        Assert.That(sagas.Sagas, Has.Count.EqualTo(1));
        
        var registration = sagas.Sagas[0];
        Assert.That(registration.SagaType, Is.EqualTo(typeof(TestSaga)));
        Assert.That(registration.SagaDataType, Is.EqualTo(typeof(TestSagaData)));
    }

    [Test]
    public void RegisterSaga_Multiple_Should_Store_All()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterSaga<TestSaga, TestSagaData>();
        config.RegisterSaga<AnotherTestSaga, AnotherTestSagaData>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredSagas sagas), Is.True);
        Assert.That(sagas.Sagas, Has.Count.EqualTo(2));
        
        Assert.That(sagas.Sagas, Has.Some.Matches<SagaRegistration>(r => r.SagaType == typeof(TestSaga)));
        Assert.That(sagas.Sagas, Has.Some.Matches<SagaRegistration>(r => r.SagaType == typeof(AnotherTestSaga)));
    }

    [Test]
    public void RegisterSaga_Null_SagaType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterSaga(null, typeof(TestSagaData)));
    }

    [Test]
    public void RegisterSaga_Null_SagaDataType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterSaga(typeof(TestSaga), null));
    }

    public class TestSagaData : ContainSagaData
    {
        public string OrderId { get; set; }
    }

    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<TestMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
        {
            mapper.ConfigureMapping<TestMessage>(message => message.OrderId)
                .ToSaga(saga => saga.OrderId);
        }

        public System.Threading.Tasks.Task Handle(TestMessage message, IMessageHandlerContext context)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    public class AnotherTestSagaData : ContainSagaData
    {
        public Guid CorrelationId { get; set; }
    }

    public class AnotherTestSaga : Saga<AnotherTestSagaData>, IAmStartedByMessages<AnotherTestMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherTestSagaData> mapper)
        {
            mapper.ConfigureMapping<AnotherTestMessage>(message => message.CorrelationId)
                .ToSaga(saga => saga.CorrelationId);
        }

        public System.Threading.Tasks.Task Handle(AnotherTestMessage message, IMessageHandlerContext context)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    public class TestMessage : IMessage
    {
        public string OrderId { get; set; }
    }

    public class AnotherTestMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
    }
}

