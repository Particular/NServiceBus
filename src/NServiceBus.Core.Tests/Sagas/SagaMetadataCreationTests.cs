namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class SagaMetadataCreationTests
{
    [Test]
    public void Throws_when_does_not_implement_generic_saga() => Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MyNonGenericSaga)));

    [Test]
    public void GetEntityClrType()
    {
        var metadata = SagaMetadata.Create(typeof(MySaga));
        Assert.That(metadata.SagaEntityType, Is.EqualTo(typeof(MySaga.MyEntity)));
    }

    [Test]
    public void GetSagaClrType()
    {
        var metadata = SagaMetadata.Create(typeof(MySaga));
        Assert.That(metadata.SagaType, Is.EqualTo(typeof(MySaga)));
    }

    [Test]
    public void DetectUniquePropertiesByAttribute()
    {
        var metadata = SagaMetadata.Create(typeof(MySaga));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void When_finder_for_non_message()
    {
        var exception = Assert.Throws<Exception>(() => { SagaMetadata.Create(typeof(SagaWithNonMessageFinder)); });
        Assert.That(exception.Message, Does.Contain(nameof(SagaWithNonMessageFinder.StartSagaMessage)));
    }

    [Test]
    public void When_message_only_has_custom_finder()
    {
        var metadata = SagaMetadata.Create(typeof(SagaWithFinderOnly));
        Assert.That(metadata.Finders.Count, Is.EqualTo(1));
        Assert.That(metadata.Finders.First().SagaFinder.GetType(), Is.EqualTo(typeof(CustomFinderAdapter<SagaWithFinderOnly.Finder, SagaWithFinderOnly.SagaData, SagaWithFinderOnly.StartSagaMessage>)));
    }

    [Test]
    public void When_a_finder_and_a_mapping_exists_for_same_property()
    {
        var exception = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithMappingAndFinder)));
        Assert.That(exception.Message, Does.Contain("mapping already exists"));
    }

    [Test]
    public void HandleBothUniqueAttributeAndMapping()
    {
        var metadata = SagaMetadata.Create(typeof(MySagaWithMappedAndUniqueProperty));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [TestCase(typeof(MySagaWithMappedProperty))]
    [TestCase(typeof(MySagaWithMappedHeader))]
    public void AutomaticallyAddUniqueForMappedProperties(Type sagaType)
    {
        var metadata = SagaMetadata.Create(sagaType);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void RequireFinderForMessagesStartingTheSaga()
    {
        var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MySagaWithUnmappedStartProperty)));

        Assert.That(ex.Message, Does.Contain(nameof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga)));
    }

    [Test]
    public void HandleNonExistingFinders()
    {
        var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MySagaWithUnmappedStartProperty)));

        Assert.That(ex.Message, Does.Contain("mapper.ConfigureMapping"));
    }

    [Test]
    public void DetectMessagesStartingTheSaga()
    {
        var metadata = SagaMetadata.Create(typeof(SagaWith2StartersAnd1Handler));

        var messages = metadata.AssociatedMessages;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messages.Count, Is.EqualTo(4));

            Assert.That(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage1).FullName), Is.True);

            Assert.That(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage2).FullName), Is.True);

            Assert.That(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.Message3).FullName), Is.False);

            Assert.That(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.MyTimeout).FullName), Is.False);
        }
    }

    [Test]
    public void DetectAndRegisterPropertyFinders()
    {
        var metadata = SagaMetadata.Create(typeof(MySagaWithMappedProperty));

        var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

        Assert.That(finder.SagaFinder.GetType(), Is.EqualTo(typeof(PropertySagaFinder<MySagaWithMappedProperty.SagaData, SomeMessage>)));
    }

    [Test]
    public void DetectAndRegisterHeaderFinders()
    {
        var metadata = SagaMetadata.Create(typeof(MySagaWithMappedHeader));

        var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

        Assert.That(finder.SagaFinder.GetType(), Is.EqualTo(typeof(HeaderPropertySagaFinder<MySagaWithMappedHeader.SagaData>)));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageProps()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToNonGuidMessageProperty)));
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdFromStringToGuidForMessagePropsThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToStringMessageProperty)));
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnNonSagaIdGuidPropertyFromStringToGuidForMessagePropsThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithNonIdPropertyMappedToStringMessageProperty)));
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageFields()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToNonGuidMessageField)));
        Assert.That(ex.Message, Does.Contain(nameof(SomeMessage)));
    }

    [Test]
    public void ValidateThatSagaPropertyIsNotAField()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithSagaDataMemberAsFieldInsteadOfProperty)));
        Assert.That(ex.Message, Does.Contain(typeof(SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData).FullName));
    }

    [TestCase(typeof(SagaThatMapsMessageItDoesntHandle))]
    [TestCase(typeof(SagaThatMapsHeaderFromMessageItDoesntHandle))]
    public void ValidateThrowsWhenSagaMapsMessageItDoesntHandle(Type sagaType)
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(sagaType));

        Assert.That(ex.Message.Contains("since the saga does not handle that message"));
    }

    [Test]
    public void ValidateThrowsWhenSagaCustomFinderMapsMessageItDoesntHandle()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create(typeof(SagaWithCustomFinderForMessageItDoesntHandle)));

        Assert.That(ex.Message.Contains(nameof(SagaWithCustomFinderForMessageItDoesntHandle.OtherMessage)) && ex.Message.Contains(nameof(SagaWithCustomFinderForMessageItDoesntHandle.Finder)));
    }

    [Test]
    public void GetEntityClrTypeFromInheritanceChain()
    {
        var metadata = SagaMetadata.Create(typeof(SagaWithInheritanceChain));

        Assert.That(metadata.SagaEntityType, Is.EqualTo(typeof(SagaWithInheritanceChain.SagaData)));
    }

    static SagaFinderDefinition GetFinder(SagaMetadata metadata, string messageType)
    {
        if (!metadata.TryGetFinder(messageType, out var finder))
        {
            throw new Exception("Finder not found");
        }

        return finder;
    }

    class MyNonGenericSaga : Saga
    {
        protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
        }
    }

    class MySaga : Saga<MySaga.MyEntity>, IAmStartedByMessages<StartingMessage>
    {
        public Task Handle(StartingMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper) => mapper.ConfigureMapping<StartingMessage>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);

        internal class MyEntity : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }


    class StartingMessage
    {
        public int UniqueProperty { get; set; }
    }

    public class SagaWithNonMessageFinder : Saga<SagaWithNonMessageFinder.SagaData>,
        IAmStartedByMessages<SagaWithNonMessageFinder.StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            //no mapping for the start message
        }

        public class SagaData : ContainSagaData
        {
            public string Property { get; set; }
        }

        public class Finder : ISagaFinder<SagaData, StartSagaMessage>
        {
            public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(SagaData));
        }

        public class StartSagaMessage
        {
            public string Property { get; set; }
        }
    }

    public class SagaWithFinderOnly : Saga<SagaWithFinderOnly.SagaData>,
        IAmStartedByMessages<SagaWithFinderOnly.StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.ConfigureFinderMapping<StartSagaMessage, Finder>();

        public class SagaData : ContainSagaData
        {
            public string Property { get; set; }
        }

        public class Finder : ISagaFinder<SagaData, StartSagaMessage>
        {
            public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(SagaData));
        }

        public class StartSagaMessage : IMessage
        {
        }
    }

    public class SagaWithMappingAndFinder : Saga<SagaWithMappingAndFinder.SagaData>,
        IAmStartedByMessages<SagaWithMappingAndFinder.StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.Property)
                .ToSaga(s => s.Property);
            mapper.ConfigureFinderMapping<StartSagaMessage, Finder>();
        }

        public class SagaData : ContainSagaData
        {
            public string Property { get; set; }
        }

        class Finder : ISagaFinder<SagaData, StartSagaMessage>
        {
            public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(SagaData));
        }

        public class StartSagaMessage : IMessage
        {
            public string Property { get; set; }
        }
    }

    class MySagaWithMappedAndUniqueProperty : Saga<MySagaWithMappedAndUniqueProperty.SagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                .ToSaga(s => s.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class MySagaWithMappedProperty : Saga<MySagaWithMappedProperty.SagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                .ToSaga(s => s.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class MySagaWithMappedHeader : Saga<MySagaWithMappedHeader.SagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureHeaderMapping<SomeMessage>("CorrelationHeader")
                .ToSaga(s => s.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class StartMessage
    {
    }

    class MySagaWithUnmappedStartProperty : Saga<MySagaWithUnmappedStartProperty.SagaData>,
        IAmStartedByMessages<MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga>,
        IHandleMessages<MySagaWithUnmappedStartProperty.MessageThatDoesNotStartTheSaga>
    {
        public Task Handle(MessageThatStartsTheSaga message, IMessageHandlerContext context) => Task.CompletedTask;

        public Task Handle(MessageThatDoesNotStartTheSaga message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            // Saga does not contain mappings on purpose, and should throw an exception
        }

        public class MessageThatStartsTheSaga : IMessage
        {
            public int SomeProperty { get; set; }
        }

        public class MessageThatDoesNotStartTheSaga : IMessage
        {
            public int SomeProperty { get; set; }
        }

        public class SagaData : ContainSagaData
        {
        }
    }


    class SagaWith2StartersAnd1Handler : Saga<SagaWith2StartersAnd1Handler.SagaData>,
        IAmStartedByMessages<SagaWith2StartersAnd1Handler.StartMessage1>,
        IAmStartedByMessages<SagaWith2StartersAnd1Handler.StartMessage2>,
        IHandleMessages<SagaWith2StartersAnd1Handler.Message3>,
        IHandleTimeouts<SagaWith2StartersAnd1Handler.MyTimeout>
    {
        public Task Handle(StartMessage1 message, IMessageHandlerContext context) => throw new NotImplementedException();

        public Task Handle(StartMessage2 message, IMessageHandlerContext context) => throw new NotImplementedException();

        public Task Handle(Message3 message, IMessageHandlerContext context) => throw new NotImplementedException();

        public Task Timeout(MyTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartMessage1>(m => m.SomeId)
                .ToSaga(s => s.SomeId);
            mapper.ConfigureMapping<StartMessage2>(m => m.SomeId)
                .ToSaga(s => s.SomeId);
        }

        public class StartMessage1 : IMessage
        {
            public string SomeId { get; set; }
        }

        public class StartMessage2 : IMessage
        {
            public string SomeId { get; set; }
        }

        public class Message3 : IMessage
        {
        }

        public class SagaData : ContainSagaData
        {
            public string SomeId { get; set; }
        }

        public class MyTimeout
        {
        }
    }

    class SagaWithIdMappedToStringMessageProperty : Saga<SagaWithIdMappedToStringMessageProperty.SagaData>,
        IAmStartedByMessages<SomeMessageWithStringProperty>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessageWithStringProperty>(m => m.StringProperty)
                .ToSaga(s => s.Id);

        public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    class SagaWithNonIdPropertyMappedToStringMessageProperty : Saga<SagaWithNonIdPropertyMappedToStringMessageProperty.SagaData>,
        IAmStartedByMessages<SomeMessageWithStringProperty>
    {
        public class SagaData : ContainSagaData
        {
            public Guid NonIdColumn { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessageWithStringProperty>(m => m.StringProperty)
                .ToSaga(s => s.NonIdColumn);

        public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    class SagaWithIdMappedToNonGuidMessageProperty : Saga<SagaWithIdMappedToNonGuidMessageProperty.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                .ToSaga(s => s.Id);

        public class SagaData : ContainSagaData
        {
        }
    }

    class SagaWithIdMappedToNonGuidMessageField : Saga<SagaWithIdMappedToNonGuidMessageField.SagaData>,
        IAmStartedByMessages<SomeMessageWithField>
    {
        public Task Handle(SomeMessageWithField message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessageWithField>(m => m.SomeProperty)
                .ToSaga(s => s.Id);

        public class SagaData : ContainSagaData
        {
        }
    }

    class SagaWithSagaDataMemberAsFieldInsteadOfProperty : Saga<SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                .ToSaga(s => s.SomeField);

        public class SagaData : ContainSagaData
        {
            public int SomeField = 0;
        }
    }

    class SagaWithInheritanceChain : SagaWithInheritanceChainBase<SagaWithInheritanceChain.SagaData, SagaWithInheritanceChain.SomeOtherData>, IAmStartedByMessages<SomeMessageWithStringProperty>
    {
        public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public string SomeId { get; set; }
        }

        public class SomeOtherData
        {
            public string SomeData { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            base.ConfigureHowToFindSaga(mapper);
            mapper.ConfigureMapping<SomeMessageWithStringProperty>(message => message.StringProperty).ToSaga(saga => saga.SomeId);
        }
    }

    class SagaThatMapsMessageItDoesntHandle : Saga<SagaThatMapsMessageItDoesntHandle.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<SomeMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
            mapper.ConfigureMapping<OtherMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
        }

        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public int SomeProperty { get; set; }
        }

        public class OtherMessage : IMessage
        {
            public int SomeProperty { get; set; }
        }
    }

    class SagaThatMapsHeaderFromMessageItDoesntHandle : Saga<SagaThatMapsHeaderFromMessageItDoesntHandle.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<SomeMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
            mapper.ConfigureMapping<OtherMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
        }

        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public int SomeProperty { get; set; }
        }

        public class OtherMessage : IMessage
        {
            public int SomeProperty { get; set; }
        }
    }

    class SagaWithCustomFinderForMessageItDoesntHandle : Saga<SagaWithCustomFinderForMessageItDoesntHandle.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<SomeMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
            mapper.ConfigureFinderMapping<OtherMessage, Finder>();
        }

        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class Finder : ISagaFinder<SagaData, OtherMessage>
        {
            public Task<SagaData> FindBy(OtherMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(SagaData));
        }

        public class SagaData : ContainSagaData
        {
            public int SomeProperty { get; set; }
        }

        public class OtherMessage : IMessage
        {
            public int SomeProperty { get; set; }
        }
    }

    class SagaWithInheritanceChainBase<T, O> : Saga<T>
        where T : class, IContainSagaData, new()
        where O : class
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<T> mapper)
        {
        }
    }
}

class SomeMessageWithField : IMessage
{
#pragma warning disable 649
    public int SomeProperty;
#pragma warning restore 649
}

class SomeMessage : IMessage
{
    public int SomeProperty { get; set; }
}

class SomeMessageWithStringProperty : IMessage
{
    public string StringProperty { get; set; }
}