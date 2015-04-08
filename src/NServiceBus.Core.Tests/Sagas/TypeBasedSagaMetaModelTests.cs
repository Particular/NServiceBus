namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class TypeBasedSagaMetaModelTests
    {
        [Test]
        public void Throws_when_does_not_implement_generic_saga()
        {
            Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(MyNonGenericSaga)));
        }

        class MyNonGenericSaga : Saga
        {
            protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
            {
            }
        }

        [Test]
        public void GetEntityClrType()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySaga));
            Assert.AreEqual(typeof(MySaga.MyEntity), metadata.SagaEntityType);
        }

        [Test]
        public void GetSagaClrType()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySaga));

            Assert.AreEqual(typeof(MySaga), metadata.SagaType);
        }

        [Test]
        public void DetectUniquePropertiesByAttribute()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySaga));
            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties.Single().Name);
        }

        class MySaga : Saga<MySaga.MyEntity>
        {
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

        [Test]
        public void When_finder_for_non_message()
        {
            var availableTypes = new List<Type>
                                 {
                                     typeof(SagaWithNonMessageFinder.Finder)
                                 };
            var exception = Assert.Throws<Exception>(() =>
            {
                TypeBasedSagaMetaModel.Create(typeof(SagaWithNonMessageFinder), availableTypes, new Conventions());
            });
            Assert.AreEqual("A custom IFindSagas must target a valid message type as defined by the message conventions. Please change 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.TypeBasedSagaMetaModelTests+SagaWithNonMessageFinder+StartSagaMessage' to a valid message type or add it to the message conventions. Finder name 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.TypeBasedSagaMetaModelTests+SagaWithNonMessageFinder+Finder'.", exception.Message);
        }

        public class SagaWithNonMessageFinder : Saga<SagaWithNonMessageFinder.SagaData>,
            IAmStartedByMessages<SagaWithNonMessageFinder.StartSagaMessage>
        {

            public void Handle(StartSagaMessage message)
            {
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
                public SagaData FindBy(StartSagaMessage message)
                {
                    return null;
                }
            }

            public class StartSagaMessage
            {
                public string Property { get; set; }
            }
        }

        [Test]
        public void When_a_finder_and_a_mapping_exists_for_same_property()
        {
            var availableTypes = new List<Type>
                                 {
                                     typeof(SagaWithMappingAndFinder.Finder)
                                 };
            var exception = Assert.Throws<Exception>(() =>
            {
                TypeBasedSagaMetaModel.Create(typeof(SagaWithMappingAndFinder), availableTypes, new Conventions());
            });
            Assert.AreEqual("A custom IFindSagas and an existing mapping where found for message 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.TypeBasedSagaMetaModelTests+SagaWithMappingAndFinder+StartSagaMessage'. Please either remove the message mapping for remove the finder. Finder name 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.TypeBasedSagaMetaModelTests+SagaWithMappingAndFinder+Finder'.", exception.Message);
        }

        public class SagaWithMappingAndFinder : Saga<SagaWithMappingAndFinder.SagaData>,
            IAmStartedByMessages<SagaWithMappingAndFinder.StartSagaMessage>
        {

            public void Handle(StartSagaMessage message)
            {
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
                public SagaData FindBy(StartSagaMessage message)
                {
                    return null;
                }
            }

            public class StartSagaMessage : IMessage
            {
                public string Property { get; set; }
            }
        }

        [Test]
        public void HandleBothUniqueAttributeAndMapping()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithMappedAndUniqueProperty));
            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties.Single().Name);
        }

        class MySagaWithMappedAndUniqueProperty : Saga<MySagaWithMappedAndUniqueProperty.SagaData>
        {
            public class SagaData : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }
        }

        [Test]
        public void AutomaticallyAddUniqueForMappedProperties()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithMappedProperty));
            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties.Single().Name);
        }

        class MySagaWithMappedProperty : Saga<MySagaWithMappedProperty.SagaData>
        {
            public class SagaData : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }
        }

        [Test, Ignore("Not sure we should enforce this yet")]
        public void RequireFinderForMessagesStartingTheSaga()
        {
            var ex = Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(MySagaWithUnmappedStartProperty)));
            Assert.True(ex.Message.Contains(typeof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga).FullName));
        }

        [Test]
        public void HandleNonExistingFinders()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithUnmappedStartProperty));
            SagaFinderDefinition finder;

            Assert.False(metadata.TryGetFinder(typeof(MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga).FullName, out finder));
        }

        class MySagaWithUnmappedStartProperty : Saga<MySagaWithUnmappedStartProperty.SagaData>,
            IAmStartedByMessages<MySagaWithUnmappedStartProperty.MessageThatStartsTheSaga>,
            IHandleMessages<MySagaWithUnmappedStartProperty.MessageThatDoesntStartTheSaga>
        {

            public class MessageThatStartsTheSaga : IMessage
            {
                public int SomeProperty { get; set; }
            }
            public class MessageThatDoesntStartTheSaga : IMessage
            {
                public int SomeProperty { get; set; }
            }

            public class SagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
            }

            public void Handle(MessageThatStartsTheSaga message)
            {
            }

            public void Handle(MessageThatDoesntStartTheSaga message)
            {
            }
        }

        [Test]
        public void DetectMessagesStartingTheSaga()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(SagaWith2StartersAnd1Handler));

            var messages = metadata.AssociatedMessages;

            Assert.AreEqual(4, messages.Count());

            Assert.True(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage1).FullName));

            Assert.True(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.StartMessage2).FullName));

            Assert.False(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.Message3).FullName));

            Assert.False(metadata.IsMessageAllowedToStartTheSaga(typeof(SagaWith2StartersAnd1Handler.MyTimeout).FullName));
        }


        class SagaWith2StartersAnd1Handler : Saga<SagaWith2StartersAnd1Handler.SagaData>,
            IAmStartedByMessages<SagaWith2StartersAnd1Handler.StartMessage1>,
            IAmStartedByMessages<SagaWith2StartersAnd1Handler.StartMessage2>,
            IHandleMessages<SagaWith2StartersAnd1Handler.Message3>,
            IHandleTimeouts<SagaWith2StartersAnd1Handler.MyTimeout>
        {

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

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage1>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
                mapper.ConfigureMapping<StartMessage2>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
            }

            public void Handle(StartMessage1 message)
            {
                throw new NotImplementedException();
            }

            public void Handle(StartMessage2 message)
            {
                throw new NotImplementedException();
            }

            public void Handle(Message3 message)
            {
                throw new NotImplementedException();
            }

            public class MyTimeout
            {
            }

            public void Timeout(MyTimeout state)
            {
            }
        }

        [Test]
        public void DetectAndRegisterPropertyFinders()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithMappedProperty));

            var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

            Assert.AreEqual(typeof(PropertySagaFinder<MySagaWithMappedProperty.SagaData>), finder.Type);
            Assert.NotNull(finder.Properties["property-accessor"]);
            Assert.AreEqual("UniqueProperty", finder.Properties["saga-property-name"]);
        }

        [Test]
        public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageProps()
        {
            var ex = Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(SagaWithIdMappedToNonGuidMessageProperty)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        class SagaWithIdMappedToNonGuidMessageProperty : Saga<SagaWithIdMappedToNonGuidMessageProperty.SagaData>,
            IAmStartedByMessages<SomeMessage>
        {

            public class SagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.Id);
            }

            public void Handle(SomeMessage message)
            {
            }
        }


        [Test]
        public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageFields()
        {
            var ex = Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(SagaWithIdMappedToNonGuidMessageField)));
            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        class SagaWithIdMappedToNonGuidMessageField : Saga<SagaWithIdMappedToNonGuidMessageField.SagaData>,
            IAmStartedByMessages<SomeMessageWithField>
        {
            public class SagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessageWithField>(m => m.SomeProperty)
                    .ToSaga(s => s.Id);
            }

            public void Handle(SomeMessageWithField message)
            {
            }
        }

        [Test]
        public void DetectAndRegisterCustomFindersUsingScanning()
        {
            var availableTypes = new List<Type>
                                 {
                                     typeof(MySagaWithScannedFinder.CustomFinder)
                                 };
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithScannedFinder), availableTypes, new Conventions());

            var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

            Assert.AreEqual(typeof(CustomFinderAdapter<MySagaWithScannedFinder.SagaData, SomeMessage>), finder.Type);
            Assert.AreEqual(typeof(MySagaWithScannedFinder.CustomFinder), finder.Properties["custom-finder-clr-type"]);
        }

        class MySagaWithScannedFinder : Saga<MySagaWithScannedFinder.SagaData>, IAmStartedByMessages<SomeMessage>
        {
            public class SagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
            }

            internal class CustomFinder : IFindSagas<SagaData>.Using<SomeMessage>
            {
                public SagaData FindBy(SomeMessage message)
                {
                    return null;
                }
            }

            public void Handle(SomeMessage message)
            {

            }
        }

        [Test]
        public void GetEntityClrTypeFromInheritanceChain()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(SagaWithInheritanceChain));

            Assert.AreEqual(typeof(SagaWithInheritanceChain.SagaData), metadata.SagaEntityType);
        }

        class SagaWithInheritanceChain : SagaWithInheritanceChainBase<SagaWithInheritanceChain.SagaData, SagaWithInheritanceChain.SomeOtherData>
        {
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

        SagaFinderDefinition GetFinder(SagaMetadata metadata, string messageType)
        {
            SagaFinderDefinition finder;

            if (!metadata.TryGetFinder(messageType, out finder))
            {
                throw new Exception("Finder not found");
            }

            return finder;
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