﻿namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

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
            Assert.AreEqual("A custom IFindSagas must target a valid message type as defined by the message conventions. Change 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithNonMessageFinder+StartSagaMessage' to a valid message type or add it to the message conventions. Finder name 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaMetadataCreationTests+SagaWithNonMessageFinder+Finder'.", exception.Message);
        }

        [Test]
        public void When_message_only_has_custom_finder()
        {
            var availableTypes = new List<Type>
            {
                typeof(SagaWithFinderOnly.Finder)
            };
            var metadata = SagaMetadata.Create(typeof(SagaWithFinderOnly), availableTypes, new Conventions());
            Assert.AreEqual(1, metadata.Finders.Count);
            Assert.AreEqual(typeof(CustomFinderAdapter<SagaWithFinderOnly.SagaData, SagaWithFinderOnly.StartSagaMessage>), metadata.Finders.First().Type);
        }

        [Test]
        [TestCase(typeof(SagaWithMappingAndFinder.Finder), typeof(SagaWithMappingAndFinder), typeof(SagaWithMappingAndFinder.StartSagaMessage))]
        [TestCase(typeof(SagaWithMappingForBaseAndFinderForDerived.Finder), typeof(SagaWithMappingForBaseAndFinderForDerived), typeof(SagaWithMappingForBaseAndFinderForDerived.StartSagaMessage))]
        [TestCase(typeof(SagaWithMappingForDerivedAndFinderForBase.Finder), typeof(SagaWithMappingForDerivedAndFinderForBase), typeof(SagaWithMappingForDerivedAndFinderForBase.StartSagaMessageBase))]
        public void When_a_finder_and_a_mapping_exists_for_same_property(Type finderType, Type sagaType, Type mappedMessageType)
        {
            var availableTypes = new List<Type>
            {
                finderType
            };
            var exception = Assert.Throws<Exception>(() => { SagaMetadata.Create(sagaType, availableTypes, new Conventions()); });
            Assert.AreEqual($"A custom IFindSagas and an existing mapping where found for message '{ mappedMessageType.FullName }'. Either remove the message mapping for remove the finder. Finder name '{ finderType.FullName }'.", exception.Message);
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
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(MySagaWithUnmappedStartProperty)));

            Assert.That(ex.Message.Contains("mapper.ConfigureMapping"));
        }

        [Test]
        public void DetectMessagesStartingTheSaga()
        {
            var metadata = SagaMetadata.Create(typeof(SagaWith2StartersAnd1Handler));

            var messages = metadata.AssociatedMessages;

            Assert.AreEqual(4, messages.Count);

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
        public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageProps()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToNonGuidMessageProperty)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void ValidateThatMappingOnSagaIdFromStringToGuidForMessagePropsThrowsException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToStringMessageProperty)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void ValidateThatMappingOnNonSagaIdGuidPropertyFromStringToGuidForMessagePropsThrowsException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithNonIdPropertyMappedToStringMessageProperty)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageFields()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithIdMappedToNonGuidMessageField)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void ValidateThatSagaPropertyIsNotAField()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SagaMetadata.Create(typeof(SagaWithSagaDataMemberAsFieldInsteadOfProperty)));
            Assert.True(ex.Message.Contains(typeof(SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData).Name));
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
        public void ValidateThrowsWhenSagaMapsMessageItDoesntHandle()
        {
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaThatMapsMessageItDoesntHandle)));

            Assert.That(ex.Message.Contains("does not handle that message") && ex.Message.Contains("in the ConfigureHowToFindSaga method"));
        }

        [Test]
        public void ValidateThrowsWhenSagaCustomFinderMapsMessageItDoesntHandle()
        {
            var availableTypes = new List<Type>
            {
                typeof(SagaWithCustomFinderForMessageItDoesntHandle.Finder)
            };
            var ex = Assert.Throws<Exception>(() => SagaMetadata.Create(typeof(SagaWithCustomFinderForMessageItDoesntHandle), availableTypes, new Conventions()));

            Assert.That(ex.Message.Contains("does not handle that message") && ex.Message.Contains("Custom saga finder"));
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

        class MySaga : Saga<MySaga.MyEntity>, IAmStartedByMessages<M1>
        {
            public Task Handle(M1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<M1>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
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
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                // Does not need a mapping for StartSagaMessage because there is a SagaFinder
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

        public class SagaWithFinderOnly : Saga<SagaWithFinderOnly.SagaData>,
            IAmStartedByMessages<SagaWithFinderOnly.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                // Does not need a mapping for StartSagaMessage because there is a SagaFinder
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

        public class SagaWithMappingAndFinder : Saga<SagaWithMappingAndFinder.SagaData>,
            IAmStartedByMessages<SagaWithMappingAndFinder.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.Property)
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

        public class SagaWithMappingForBaseAndFinderForDerived : Saga<SagaWithMappingForBaseAndFinderForDerived.SagaData>,
            IAmStartedByMessages<SagaWithMappingForBaseAndFinderForDerived.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessageBase>(m => m.Property)
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

            public class StartSagaMessageBase : IMessage
            {
                public string Property { get; set; }
            }

            public class StartSagaMessage : StartSagaMessageBase
            {
            }
        }

        public class SagaWithMappingForDerivedAndFinderForBase : Saga<SagaWithMappingForDerivedAndFinderForBase.SagaData>,
    IAmStartedByMessages<SagaWithMappingForDerivedAndFinderForBase.StartSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.Property)
                    .ToSaga(s => s.Property);
            }

            public class SagaData : ContainSagaData
            {
                public string Property { get; set; }
            }

            public class Finder : IFindSagas<SagaData>.Using<StartSagaMessageBase>
            {
                public Task<SagaData> FindBy(StartSagaMessageBase message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    return Task.FromResult(default(SagaData));
                }
            }

            public class StartSagaMessageBase : IMessage
            {
                public string Property { get; set; }
            }

            public class StartSagaMessage : StartSagaMessageBase
            {
            }
        }

        class MySagaWithMappedAndUniqueProperty : Saga<MySagaWithMappedAndUniqueProperty.SagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }

            public class SagaData : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }
        }

        class MySagaWithMappedProperty : Saga<MySagaWithMappedProperty.SagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
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
                return TaskEx.CompletedTask;
            }

            public Task Handle(MessageThatDoesNotStartTheSaga message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

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
                return TaskEx.CompletedTask;
            }

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

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessageWithStringProperty>(m => m.StringProperty)
                    .ToSaga(s => s.Id);
            }

            public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class SagaWithNonIdPropertyMappedToStringMessageProperty : Saga<SagaWithNonIdPropertyMappedToStringMessageProperty.SagaData>,
            IAmStartedByMessages<SomeMessageWithStringProperty>
        {
            public class SagaData : ContainSagaData
            {
                public Guid NonIdColumn { get; set; }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessageWithStringProperty>(m => m.StringProperty)
                    .ToSaga(s => s.NonIdColumn);
            }

            public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class SagaWithIdMappedToNonGuidMessageProperty : Saga<SagaWithIdMappedToNonGuidMessageProperty.SagaData>,
            IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.Id);
            }

            public class SagaData : ContainSagaData
            {
            }
        }

        class SagaWithIdMappedToNonGuidMessageField : Saga<SagaWithIdMappedToNonGuidMessageField.SagaData>,
            IAmStartedByMessages<SomeMessageWithField>
        {
            public Task Handle(SomeMessageWithField message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessageWithField>(m => m.SomeProperty)
                    .ToSaga(s => s.Id);
            }

            public class SagaData : ContainSagaData
            {
            }
        }

        class SagaWithSagaDataMemberAsFieldInsteadOfProperty : Saga<SagaWithSagaDataMemberAsFieldInsteadOfProperty.SagaData>,
            IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.SomeField);
            }

            public class SagaData : ContainSagaData
            {
                public int SomeField = 0;
            }
        }

        class MySagaWithScannedFinder : Saga<MySagaWithScannedFinder.SagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                // Does not need a mapping for SomeMessage because a saga finder exists
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

        class SagaWithInheritanceChain : SagaWithInheritanceChainBase<SagaWithInheritanceChain.SagaData, SagaWithInheritanceChain.SomeOtherData>, IAmStartedByMessages<SomeMessageWithStringProperty>
        {
            public Task Handle(SomeMessageWithStringProperty message, IMessageHandlerContext context)
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

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
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

        class SagaWithCustomFinderForMessageItDoesntHandle : Saga<SagaWithCustomFinderForMessageItDoesntHandle.SagaData>,
            IAmStartedByMessages<SomeMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(msg => msg.SomeProperty).ToSaga(saga => saga.SomeProperty);
            }

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public class Finder : IFindSagas<SagaData>.Using<OtherMessage>
            {
                public Task<SagaData> FindBy(OtherMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    return Task.FromResult(default(SagaData));
                }
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

    class SomeMessageWithStringProperty : IMessage
    {
        public string StringProperty { get; set; }
    }
}