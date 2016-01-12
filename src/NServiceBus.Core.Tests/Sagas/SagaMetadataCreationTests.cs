namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class SagaMetadataCreationTests
    {
        [Test]
        public void Throws_when_does_not_implement_generic_saga()
        {
            Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MyNonGenericSaga)));
        }

        [Test]
        public void GetEntityClrType()
        {
            var metadata = SagaMetadata.Create(typeof(MySaga));
            Assert.AreEqual(typeof(MySaga.MyEntity), metadata.SagaEntityType);
        }

        [Test]
        public void GetSagaClrType()
        {
            var metadata = SagaMetadata.Create(typeof(MySaga));

            Assert.AreEqual(typeof(MySaga), metadata.SagaType);
        }

        [Test]
        public void DetectUniquePropertiesByAttribute()
        {
            var metadata = SagaMetadata.Create(typeof(MySaga));

            SagaMetadata.CorrelationPropertyMetadata correlatedProperty;

            Assert.True(metadata.TryGetCorrelationProperty(out correlatedProperty));
            Assert.AreEqual("UniqueProperty", correlatedProperty.Name);
        }

        [Test]
        public void When_finder_for_non_message()
        {
            var availableTypes = new List<Type>
            {
                typeof(SagaWithNonMessageFinder.Finder)
            };
            var exception = Assert.Throws<Exception>(() => { SagaMetadata.Create(typeof(SagaWithNonMessageFinder), availableTypes, new Conventions()); });
            Assert.AreEqual("A custom IFindSagas must target a valid message type as defined by the message conventions. Please change 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithNonMessageFinder+StartSagaMessage' to a valid message type or add it to the message conventions. Finder name 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithNonMessageFinder+Finder'.", exception.Message);
        }

        [Test]
        public void When_a_finder_and_a_mapping_exists_for_same_property()
        {
            var availableTypes = new List<Type>
            {
                typeof(SagaWithMappingAndFinder.Finder)
            };
            var exception = Assert.Throws<Exception>(() => { SagaMetadata.Create(typeof(SagaWithMappingAndFinder), availableTypes, new Conventions()); });
            Assert.AreEqual("A custom IFindSagas and an existing mapping where found for message 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithMappingAndFinder+StartSagaMessage'. Please either remove the message mapping for remove the finder. Finder name 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithMappingAndFinder+Finder'.", exception.Message);
        }

        [Test]
        public void HandleBothUniqueAttributeAndMapping()
        {
            var metadata = SagaMetadata.Create(typeof(MySagaWithMappedAndUniqueProperty));

            SagaMetadata.CorrelationPropertyMetadata correlatedProperty;

            Assert.True(metadata.TryGetCorrelationProperty(out correlatedProperty));
            Assert.AreEqual("UniqueProperty", correlatedProperty.Name);
        }

        [Test]
        public void AutomaticallyAddUniqueForMappedProperties()
        {
            var metadata = SagaMetadata.Create(typeof(MySagaWithMappedProperty));
            SagaMetadata.CorrelationPropertyMetadata correlatedProperty;

            Assert.True(metadata.TryGetCorrelationProperty(out correlatedProperty));
            Assert.AreEqual("UniqueProperty", correlatedProperty.Name);
        }

        [Test, Ignore("Not sure we should enforce this yet")]
        public void RequireFinderForMessagesStartingTheSaga()
        {
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MySagaWithUnmappedStartProperty)));
            Assert.True(ex.Message.Contains(typeof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga).FullName));
        }

        [Test]
        public void HandleNonExistingFinders()
        {
            var metadata = SagaMetadata.Create(typeof(MySagaWithUnmappedStartProperty));
            SagaFinderDefinition finder;

            Assert.False(metadata.TryGetFinder(typeof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga).FullName, out finder));
        }

        [Test]
        public void DetectMessagesStartingTheSaga()
        {
            var metadata = SagaMetadata.Create(typeof(SagaWith2StartersAnd1Handler));

            var messages = metadata.AssociatedMessages;

            Assert.AreEqual(4, messages.Count());

            Assert.True(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage1).FullName));

            Assert.True(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage2).FullName));

            Assert.False(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.Message3).FullName));

            Assert.False(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.MyTimeout).FullName));
        }

        [Test]
        public void DetectAndRegisterPropertyFinders()
        {
            var metadata = SagaMetadata.Create(typeof(MySagaWithMappedProperty));

            var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

            Assert.AreEqual(typeof(PropertySagaFinder<MySagaWithMappedProperty.SagaData>), finder.Type);
            Assert.NotNull(finder.Properties["property-accessor"]);
            Assert.AreEqual("UniqueProperty", finder.Properties["saga-property-name"]);
        }

        [Test]
        public void DetectAndRegisterCustomFindersUsingScanning()
        {
            var availableTypes = new List<Type>
            {
                typeof(MySagaWithScannedFinder.CustomFinder)
            };
            var metadata = SagaMetadata.Create(typeof(MySagaWithScannedFinder), availableTypes, new Conventions());

            var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

            Assert.AreEqual(typeof(CustomFinderAdapter<MySagaWithScannedFinder.SagaData, SomeMessage>), finder.Type);
            Assert.AreEqual(typeof(MySagaWithScannedFinder.CustomFinder), finder.Properties["custom-finder-clr-type"]);
        }

        [Test]
        public void GetEntityClrTypeFromInheritanceChain()
        {
            var metadata = SagaMetadata.Create(typeof(SagaWithInheritanceChain));

            Assert.AreEqual(typeof(SagaWithInheritanceChain.SagaData), metadata.SagaEntityType);
        }

        SagaFinderDefinition GetFinder(SagaMetadata metadata, string messageType)
        {
            SagaFinderDefinition finder;

            if (!metadata.TryGetFinder(messageType, out finder))
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

        class MySaga : Saga<MySaga.MyEntity>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<M1, int>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
            }

            internal class MyEntity : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }
        }


        class M1
        {
            public int UniqueProperty { get; set; }
        }

        public class SagaWithNonMessageFinder : Saga<SagaWithNonMessageFinder.SagaData>,
            IAmStartedByMessages<SagaWithNonMessageFinder.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
            }

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
            }

            public class Finder : IFindSagas<SagaData>.Using<StartSagaMessage>
            {
                public Task<SagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    return Task.FromResult(default(SagaData));
                }
            }

            public class StartSagaMessage
            {
                public string Property { get; set; }
            }
        }

        public class SagaWithMappingAndFinder : Saga<SagaWithMappingAndFinder.SagaData>,
            IAmStartedByMessages<SagaWithMappingAndFinder.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage, string>(m => m.Property)
                    .ToSaga(s => s.Property);
            }

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
            }

            public class Finder : IFindSagas<SagaData>.Using<StartSagaMessage>
            {
                public Task<SagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    return Task.FromResult(default(SagaData));
                }
            }

            public class StartSagaMessage : IMessage
            {
                public string Property { get; set; }
            }
        }

        class MySagaWithMappedAndUniqueProperty : Saga<MySagaWithMappedAndUniqueProperty.SagaData>, IAmStartedByMessages<MySagaWithMappedAndUniqueProperty.StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage, int>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }

            public class SagaData : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }

            public class StartMessage
            {
            }
        }

        class MySagaWithMappedProperty : Saga<MySagaWithMappedProperty.SagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage, int>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }

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
            public Task Handle(MessageThatStartsTheSaga message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }

            public Task Handle(MessageThatDoesNotStartTheSaga message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
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
            public Task Handle(StartMessage1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public Task Handle(StartMessage2 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public Task Handle(Message3 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public Task Timeout(MyTimeout state, IMessageHandlerContext context)
            {
                return TaskEx.Completed;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage1, string>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
                mapper.ConfigureMapping<StartMessage2, string>(m => m.SomeId)
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

        class MySagaWithScannedFinder : Saga<MySagaWithScannedFinder.SagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
            }

            public class SagaData : ContainSagaData
            {
            }

            internal class CustomFinder : IFindSagas<SagaData>.Using<SomeMessage>
            {
                public Task<SagaData> FindBy(SomeMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    return Task.FromResult(default(SagaData));
                }
            }
        }

        class SagaWithInheritanceChain : SagaWithInheritanceChainBase<SagaWithInheritanceChain.SagaData, SagaWithInheritanceChain.SomeOtherData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public class SagaData : ContainSagaData
            {
                public string SomeId { get; set; }
            }

            public class SomeOtherData
            {
                public string SomeData { get; set; }
            }
        }

        class SagaWithInheritanceChainBase<T, O> : Saga<T>
            where T : IContainSagaData, new()
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
}