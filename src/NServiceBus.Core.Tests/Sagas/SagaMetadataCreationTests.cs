namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Fakes;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class SagaMetadataCreationTests
{
    [Test]
    public void Throws_when_does_not_implement_generic_saga() => Assert.Throws<Exception>(() => SagaMetadata.Create<MyNonGenericSaga>());

    [Test]
    public void GetEntityClrType()
    {
        var metadata = SagaMetadata.Create<MySaga>();
        Assert.That(metadata.SagaEntityType, Is.EqualTo(typeof(MySaga.MyEntity)));
    }

    [Test]
    public void GetSagaClrType()
    {
        var metadata = SagaMetadata.Create<MySaga>();
        Assert.That(metadata.SagaType, Is.EqualTo(typeof(MySaga)));
    }

    [Test]
    public void DetectUniquePropertiesByAttribute()
    {
        var metadata = SagaMetadata.Create<MySaga>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void When_finder_for_non_message()
    {
        var exception = Assert.Throws<ArgumentException>(() => { SagaMetadata.Create<SagaWithNonMessageFinder>(); });
        Assert.That(exception.Message, Does.Contain(nameof(SagaWithNonMessageFinder.OtherMessage)));
    }

    [Test]
    public void When_message_only_has_custom_finder()
    {
        var metadata = SagaMetadata.Create<SagaWithFinderOnly>();
        Assert.That(metadata.Finders.Count, Is.EqualTo(1));
        Assert.That(metadata.Finders.First().SagaFinder.GetType(), Is.EqualTo(typeof(CustomFinderAdapter<SagaWithFinderOnly.Finder, SagaWithFinderOnly.SagaData, SagaWithFinderOnly.StartSagaMessage>)));
    }

    [Test]
    public void When_a_finder_and_a_mapping_exists_for_same_property()
    {
        var exception = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithMappingAndFinder>());
        Assert.That(exception.Message, Does.Contain("mapping already exists"));
    }

    [Test]
    public void HandleBothUniqueAttributeAndMapping()
    {
        var metadata = SagaMetadata.Create<MySagaWithMappedAndUniqueProperty>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void AutomaticallyAddUniqueForMappedProperty()
    {
        var metadata = SagaMetadata.Create<MySagaWithMappedProperty>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void AutomaticallyAddUniqueForMappedHeader()
    {
        var metadata = SagaMetadata.Create<MySagaWithMappedHeader>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }
    }

    [Test]
    public void RequireFinderForMessagesStartingTheSaga()
    {
        var ex = Assert.Throws<Exception>(() => SagaMetadata.Create<MySagaWithUnmappedStartProperty>());

        Assert.That(ex.Message, Does.Contain(nameof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga)));
    }

    [Test]
    public void HandleNonExistingFinders()
    {
        var ex = Assert.Throws<Exception>(() => SagaMetadata.Create<MySagaWithUnmappedStartProperty>());

        Assert.That(ex.Message, Does.Contain("mapper.MapSaga"));
    }

    [Test]
    public void DetectMessagesStartingTheSaga()
    {
        var metadata = SagaMetadata.Create<SagaWith2StartersAnd1Handler>();

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
        var metadata = SagaMetadata.Create<MySagaWithMappedProperty>();

        var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

        Assert.That(finder.SagaFinder.GetType(), Is.EqualTo(typeof(PropertySagaFinder<MySagaWithMappedProperty.SagaData, SomeMessage>)));
    }

    [Test]
    public void DetectAndRegisterHeaderFinders()
    {
        var metadata = SagaMetadata.Create<MySagaWithMappedHeader>();

        var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

        Assert.That(finder.SagaFinder.GetType(), Is.EqualTo(typeof(HeaderPropertySagaFinder<MySagaWithMappedHeader.SagaData>)));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageProps()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithIdMappedToNonGuidMessageProperty>());
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdFromStringToGuidForMessagePropsThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithIdMappedToStringMessageProperty>());
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnNonSagaIdGuidPropertyFromStringToGuidForMessagePropsThrowsException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithNonIdPropertyMappedToStringMessageProperty>());
        Assert.That(ex.Message, Does.Contain(typeof(SomeMessage).FullName));
    }

    [Test]
    public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageFields()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithIdMappedToNonGuidMessageField>());
        Assert.That(ex.Message, Does.Contain(nameof(SomeMessage)));
    }

    [Test]
    public void ValidateThatSagaPropertyIsNotAField()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithSagaDataMemberAsFieldInsteadOfProperty>());
        Assert.That(ex.Message, Does.Contain(typeof(SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData).FullName));
    }

    [Test]
    public void ValidateThrowsWhenSagaMapsMessageItDoesntHandle()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaThatMapsMessageItDoesntHandle>());

        Assert.That(ex.Message.Contains("since the saga does not handle that message"));
    }

    [Test]
    public void ValidateThrowsWhenSagaMapsMessageUItDoesntHandleUsingHeaders()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaThatMapsHeaderFromMessageItDoesntHandle>());

        Assert.That(ex.Message.Contains("since the saga does not handle that message"));
    }

    [Test]
    public void ValidateThrowsWhenSagaCustomFinderMapsMessageItDoesntHandle()
    {
        var ex = Assert.Throws<ArgumentException>(() => SagaMetadata.Create<SagaWithCustomFinderForMessageItDoesntHandle>());

        Assert.That(ex.Message.Contains(nameof(SagaWithCustomFinderForMessageItDoesntHandle.OtherMessage)) && ex.Message.Contains(nameof(SagaWithCustomFinderForMessageItDoesntHandle.Finder)));
    }

    [Test]
    public void GetEntityClrTypeFromInheritanceChain()
    {
        var metadata = SagaMetadata.Create<SagaWithInheritanceChain>();

        Assert.That(metadata.SagaEntityType, Is.EqualTo(typeof(SagaWithInheritanceChain.SagaData)));
    }

    static IEnumerable<TestCaseData> PropertyFinderValueCases()
    {
        yield return new TestCaseData(typeof(PropertySagaWithStringMapping), new StringPropertyMessage { UniqueProperty = "some-value" }, "some-value");
        yield return new TestCaseData(typeof(PropertySagaWithGuidMapping), new GuidPropertyMessage { UniqueProperty = new Guid("d5c9a3e7-8b2f-4a1e-9c6d-3f8b2a5e7c1d") }, new Guid("d5c9a3e7-8b2f-4a1e-9c6d-3f8b2a5e7c1d"));
        yield return new TestCaseData(typeof(PropertySagaWithLongMapping), new LongPropertyMessage { UniqueProperty = 456L }, 456L);
        yield return new TestCaseData(typeof(PropertySagaWithULongMapping), new ULongPropertyMessage { UniqueProperty = 456UL }, 456UL);
        yield return new TestCaseData(typeof(PropertySagaWithIntMapping), new IntPropertyMessage { UniqueProperty = 456 }, 456);
        yield return new TestCaseData(typeof(PropertySagaWithUIntMapping), new UIntPropertyMessage { UniqueProperty = 456U }, 456U);
        yield return new TestCaseData(typeof(PropertySagaWithShortMapping), new ShortPropertyMessage { UniqueProperty = 456 }, (short)456);
        yield return new TestCaseData(typeof(PropertySagaWithUShortMapping), new UShortPropertyMessage { UniqueProperty = 456 }, (ushort)456);
    }

    [TestCaseSource(nameof(PropertyFinderValueCases))]
    public async Task PropertyFinder_UsesExpressionsByDefault(Type sagaType, object message, object expectedValue)
    {
        var services = new ServiceCollection();
        var fakeSagaPersister = new FakeSagaPersister();
        services.AddSingleton<ISagaPersister>(fakeSagaPersister);
        await using var provider = services.BuildServiceProvider();

        var finder = SagaMetadata.CreateMany([sagaType]).Single().Finders.Single();

        await finder.SagaFinder.Find(provider, new FakeSynchronizedStorageSession(), new ContextBag(), message, new Dictionary<string, string>()).ConfigureAwait(false);

        Assert.That(fakeSagaPersister.PropertyValue, Is.EqualTo(expectedValue).And.TypeOf(expectedValue.GetType()));
    }

    // Not testing type permutations here because that would only exercise the
    // test-local unsafe accessors, not production code paths.
    [Test]
    public async Task PropertyFinder_AllowsPassingAccessor()
    {
        var services = new ServiceCollection();
        var fakeSagaPersister = new FakeSagaPersister();
        services.AddSingleton<ISagaPersister>(fakeSagaPersister);
        await using var provider = services.BuildServiceProvider();

        var metadata = SagaMetadata.Create<MySaga, MySaga.MyEntity>([new SagaMessage(typeof(StartingMessage), true, false)], propertyAccessors: [new StartingMessageAccessor()]);
        var finder = metadata.Finders.Single();

        var startingMessage = new StartingMessage { UniqueProperty = 123 };

        await finder.SagaFinder.Find(provider, new FakeSynchronizedStorageSession(), new ContextBag(), startingMessage, new Dictionary<string, string>()).ConfigureAwait(false);

        Assert.That(fakeSagaPersister.PropertyValue, Is.EqualTo(startingMessage.UniqueProperty).And.TypeOf(startingMessage.UniqueProperty.GetType()));
    }

    static IEnumerable<TestCaseData> CorrelationHeaderValueCases()
    {
        yield return new TestCaseData(typeof(SagaWithStringHeaderMapping), "some-value", "some-value");
        yield return new TestCaseData(typeof(SagaWithGuidHeaderMapping), "d5c9a3e7-8b2f-4a1e-9c6d-3f8b2a5e7c1d", new Guid("d5c9a3e7-8b2f-4a1e-9c6d-3f8b2a5e7c1d"));
        yield return new TestCaseData(typeof(SagaWithLongHeaderMapping), "456", (long)456);
        yield return new TestCaseData(typeof(SagaWithULongHeaderMapping), "456", (ulong)456);
        yield return new TestCaseData(typeof(SagaWithIntHeaderMapping), "456", 456);
        yield return new TestCaseData(typeof(SagaWithUIntHeaderMapping), "456", (uint)456);
        yield return new TestCaseData(typeof(SagaWithShortHeaderMapping), "456", (short)456);
        yield return new TestCaseData(typeof(SagaWithUShortHeaderMapping), "456", (ushort)456);
    }

    [TestCaseSource(nameof(CorrelationHeaderValueCases))]
    public async Task HeaderFinder_ConvertsCorrelationHeaderValue(Type sagaType, string headerValue, object expectedValue)
    {
        var services = new ServiceCollection();
        var fakeSagaPersister = new FakeSagaPersister();
        services.AddSingleton<ISagaPersister>(fakeSagaPersister);
        await using var provider = services.BuildServiceProvider();

        // Using the non-trimming friendly path here for the test
        var finder = SagaMetadata.CreateMany([sagaType]).Single().Finders.Single();

        var headers = new Dictionary<string, string> { { "CorrelationHeader", headerValue } };

        await finder.SagaFinder.Find(provider, new FakeSynchronizedStorageSession(), new ContextBag(), new HeaderTestMessage(), headers).ConfigureAwait(false);

        Assert.That(fakeSagaPersister.PropertyValue, Is.EqualTo(expectedValue).And.TypeOf(expectedValue.GetType()));
    }

    class FakeSagaPersister : ISagaPersister
    {
        public object PropertyValue { get; set; }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session,
            ContextBag context, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context,
            CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData =>
            throw new NotImplementedException();

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context,
            CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
        {
            PropertyValue = propertyValue;
            return Task.FromResult(default(TSagaData));
        }

        public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    class StartingMessageAccessor : MessagePropertyAccessor<StartingMessage>
    {
        protected override object AccessFrom(StartingMessage message) => AccessFrom_UniqueProperty(message);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UniqueProperty")]
        static extern int AccessFrom_UniqueProperty(StartingMessage unsafeExample);
    }

    static SagaFinderDefinition GetFinder(SagaMetadata metadata, string messageType)
    {
        if (!metadata.TryGetFinder(messageType, out var finder))
        {
            throw new Exception("Finder not found");
        }

        return finder;
    }

    class PropertySagaWithStringMapping : Saga<PropertySagaWithStringMapping.SagaData>, IAmStartedByMessages<StringPropertyMessage>
    {
        public Task Handle(StringPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<StringPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public string UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithGuidMapping : Saga<PropertySagaWithGuidMapping.SagaData>, IAmStartedByMessages<GuidPropertyMessage>
    {
        public Task Handle(GuidPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<GuidPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public Guid UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithLongMapping : Saga<PropertySagaWithLongMapping.SagaData>, IAmStartedByMessages<LongPropertyMessage>
    {
        public Task Handle(LongPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<LongPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public long UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithULongMapping : Saga<PropertySagaWithULongMapping.SagaData>, IAmStartedByMessages<ULongPropertyMessage>
    {
        public Task Handle(ULongPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<ULongPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public ulong UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithIntMapping : Saga<PropertySagaWithIntMapping.SagaData>, IAmStartedByMessages<IntPropertyMessage>
    {
        public Task Handle(IntPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<IntPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithUIntMapping : Saga<PropertySagaWithUIntMapping.SagaData>, IAmStartedByMessages<UIntPropertyMessage>
    {
        public Task Handle(UIntPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<UIntPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public uint UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithShortMapping : Saga<PropertySagaWithShortMapping.SagaData>, IAmStartedByMessages<ShortPropertyMessage>
    {
        public Task Handle(ShortPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<ShortPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public short UniqueProperty { get; set; }
        }
    }

    class PropertySagaWithUShortMapping : Saga<PropertySagaWithUShortMapping.SagaData>, IAmStartedByMessages<UShortPropertyMessage>
    {
        public Task Handle(UShortPropertyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<UShortPropertyMessage>(m => m.UniqueProperty);

        public class SagaData : ContainSagaData
        {
            public ushort UniqueProperty { get; set; }
        }
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

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper) => mapper.MapSaga(s => s.UniqueProperty).ToMessage<StartingMessage>(m => m.UniqueProperty);

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

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.ConfigureFinderMapping<OtherMessage, Finder>();

        public class SagaData : ContainSagaData;

        public class Finder : ISagaFinder<SagaData, OtherMessage>
        {
            public Task<SagaData> FindBy(OtherMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(default(SagaData));
        }

        public class StartSagaMessage;

        public class OtherMessage;
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

        public class StartSagaMessage : IMessage;
    }

    public class SagaWithMappingAndFinder : Saga<SagaWithMappingAndFinder.SagaData>,
        IAmStartedByMessages<SagaWithMappingAndFinder.StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.MapSaga(s => s.Property).ToMessage<StartSagaMessage>(m => m.Property);
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
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<SomeMessage>(m => m.SomeProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class MySagaWithMappedProperty : Saga<MySagaWithMappedProperty.SagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessage<SomeMessage>(m => m.SomeProperty);

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class MySagaWithMappedHeader : Saga<MySagaWithMappedHeader.SagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<SomeMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class SagaWithStringHeaderMapping : Saga<SagaWithStringHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public string UniqueProperty { get; set; }
        }
    }

    class SagaWithGuidHeaderMapping : Saga<SagaWithGuidHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public Guid UniqueProperty { get; set; }
        }
    }

    class SagaWithLongHeaderMapping : Saga<SagaWithLongHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public long UniqueProperty { get; set; }
        }
    }

    class SagaWithULongHeaderMapping : Saga<SagaWithULongHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public ulong UniqueProperty { get; set; }
        }
    }

    class SagaWithIntHeaderMapping : Saga<SagaWithIntHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }
    }

    class SagaWithUIntHeaderMapping : Saga<SagaWithUIntHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public uint UniqueProperty { get; set; }
        }
    }

    class SagaWithShortHeaderMapping : Saga<SagaWithShortHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public short UniqueProperty { get; set; }
        }
    }

    class SagaWithUShortHeaderMapping : Saga<SagaWithUShortHeaderMapping.SagaData>, IAmStartedByMessages<HeaderTestMessage>
    {
        public Task Handle(HeaderTestMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.UniqueProperty).ToMessageHeader<HeaderTestMessage>("CorrelationHeader");

        public class SagaData : ContainSagaData
        {
            public ushort UniqueProperty { get; set; }
        }
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

        public class SagaData : ContainSagaData;
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

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.SomeId)
                .ToMessage<StartMessage1>(m => m.SomeId)
                .ToMessage<StartMessage2>(m => m.SomeId);

        public class StartMessage1 : IMessage
        {
            public string SomeId { get; set; }
        }

        public class StartMessage2 : IMessage
        {
            public string SomeId { get; set; }
        }

        public class Message3 : IMessage;

        public class SagaData : ContainSagaData
        {
            public string SomeId { get; set; }
        }

        public class MyTimeout;
    }

    class SagaWithIdMappedToStringMessageProperty : Saga<SagaWithIdMappedToStringMessageProperty.SagaData>,
        IAmStartedByMessages<SomeMessageWithStringProperty>
    {
        public class SagaData : ContainSagaData;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.Id).ToMessage<SomeMessageWithStringProperty>(m => m.StringProperty);

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
            mapper.MapSaga(s => s.NonIdColumn).ToMessage<SomeMessageWithStringProperty>(m => m.StringProperty);

        public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    class SagaWithIdMappedToNonGuidMessageProperty : Saga<SagaWithIdMappedToNonGuidMessageProperty.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.Id).ToMessage<SomeMessage>(m => m.SomeProperty);

        public class SagaData : ContainSagaData;
    }

    class SagaWithIdMappedToNonGuidMessageField : Saga<SagaWithIdMappedToNonGuidMessageField.SagaData>,
        IAmStartedByMessages<SomeMessageWithField>
    {
        public Task Handle(SomeMessageWithField message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.Id).ToMessage<SomeMessageWithField>(m => m.SomeProperty);

        public class SagaData : ContainSagaData;
    }

    class SagaWithSagaDataMemberAsFieldInsteadOfProperty : Saga<SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.SomeField).ToMessage<SomeMessage>(m => m.SomeProperty);

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
            mapper.MapSaga(s => s.SomeId).ToMessage<SomeMessageWithStringProperty>(message => message.StringProperty);
        }
    }

    class SagaThatMapsMessageItDoesntHandle : Saga<SagaThatMapsMessageItDoesntHandle.SagaData>,
        IAmStartedByMessages<SomeMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.SomeProperty)
                .ToMessage<SomeMessage>(m => m.SomeProperty)
                .ToMessage<OtherMessage>(m => m.SomeProperty);

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
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(s => s.SomeProperty)
                .ToMessage<SomeMessage>(m => m.SomeProperty)
                .ToMessageHeader<OtherMessage>("MyHeader");

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
            mapper.MapSaga(saga => saga.SomeProperty).ToMessage<SomeMessage>(m => m.SomeProperty);
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

class HeaderTestMessage : IMessage;

class StringPropertyMessage : IMessage
{
    public string UniqueProperty { get; set; }
}

class GuidPropertyMessage : IMessage
{
    public Guid UniqueProperty { get; set; }
}

class LongPropertyMessage : IMessage
{
    public long UniqueProperty { get; set; }
}

class ULongPropertyMessage : IMessage
{
    public ulong UniqueProperty { get; set; }
}

class IntPropertyMessage : IMessage
{
    public int UniqueProperty { get; set; }
}

class UIntPropertyMessage : IMessage
{
    public uint UniqueProperty { get; set; }
}

class ShortPropertyMessage : IMessage
{
    public short UniqueProperty { get; set; }
}

class UShortPropertyMessage : IMessage
{
    public ushort UniqueProperty { get; set; }
}

class SomeMessageWithStringProperty : IMessage
{
    public string StringProperty { get; set; }
}