namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class TypeBasedSagaMetaModelTests
    {


        [Test]
        public void GetEntityClrType()
        {

            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySaga));


            Assert.AreEqual(typeof(MyEntity), metadata.SagaEntityType);
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

        [Test]
        public void HandleBothUniqueAttributeAndMapping()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithMappedAndUniqueProperty));


            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties.Single().Name);
        }

        [Test]
        public void AutomaticallyAddUniqueForMappedProperties()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithMappedProperty));


            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties.Single().Name);
        }

        [Test, Ignore("Not sure we should enforce this yet")]
        public void RequireFinderForMessagesStartingTheSaga()
        {
            var ex = Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(MySagaWithUnmappedStartProperty)));


            Assert.True(ex.Message.Contains(typeof(MessageThatStartsTheSaga).FullName));
        }

        [Test]
        public void HandleNonExistingFinders()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithUnmappedStartProperty));
            SagaFinderDefinition finder;

            Assert.False(metadata.TryGetFinder(typeof(MessageThatStartsTheSaga).FullName, out finder));
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
            var ex = Assert.Throws<Exception>(()=>TypeBasedSagaMetaModel.Create(typeof(SagaWithIdMappedToNonGuidMessageProperty)));


            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void ValidateThatMappingOnSagaIdHasTypeGuidForMessageFields()
        {
            var ex = Assert.Throws<Exception>(() => TypeBasedSagaMetaModel.Create(typeof(SagaWithIdMappedToNonGuidMessageField)));


            Assert.True(ex.Message.Contains(typeof(SomeMessage).Name));
        }

        [Test]
        public void DetectAndRegisterCustomFindersUsingScanning()
        {
            var metadata = TypeBasedSagaMetaModel.Create(typeof(MySagaWithScannedFinder),
                new List<Type>
                {
                    typeof(MySagaWithScannedFinder.CustomFinder)
                }, new Conventions());

            var finder = GetFinder(metadata, typeof(SomeMessage).FullName);

            Assert.AreEqual(typeof(CustomFinderAdapter<MySagaWithScannedFinder.SagaData, SomeMessage>), finder.Type);
            Assert.AreEqual(typeof(MySagaWithScannedFinder.CustomFinder), finder.Properties["custom-finder-clr-type"]);
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

        class MySagaWithMappedAndUniqueProperty : Saga<MySagaWithMappedAndUniqueProperty.SagaData>
        {
            public class SagaData : ContainSagaData
            {
                [Unique]
                public int UniqueProperty { get; set; }
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m => m.SomeProperty)
                    .ToSaga(s => s.UniqueProperty);
            }
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


        class MySaga : Saga<MyEntity>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {

            }
        }

        class MyEntity : ContainSagaData
        {
            [Unique]
            public int UniqueProperty { get; set; }
        }

        class MySagaWithUnmappedStartProperty : Saga<MySagaWithUnmappedStartProperty.SagaData>,
            IAmStartedByMessages<MessageThatStartsTheSaga>,
            IHandleMessages<MessageThatDoesntStartTheSaga>
        {


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

        class SagaWithIdMappedToNonGuidMessageProperty : Saga<SagaWithIdMappedToNonGuidMessageProperty.SagaData>,
           IAmStartedByMessages<SomeMessage>
        {


            public class SagaData : ContainSagaData
            {

            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessage>(m=>m.SomeProperty)
                    .ToSaga(s=>s.Id);
            }


            public void Handle(SomeMessage message)
            {
                
            }
        }

         class SagaWithIdMappedToNonGuidMessageField : Saga<SagaWithIdMappedToNonGuidMessageField.SagaData>,
           IAmStartedByMessages<SomeMessageWithField>
        {


            public class SagaData : ContainSagaData
            {

            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<SomeMessageWithField>(m=>m.SomeProperty)
                    .ToSaga(s=>s.Id);
            }


            public void Handle(SomeMessageWithField message)
            {
                
            }
        }
    }

    class SomeMessageWithField:IMessage
    {
#pragma warning disable 649
        public int SomeProperty;
#pragma warning restore 649
    }

    class SomeMessage : IMessage
    {
        public int SomeProperty { get; set; }
    }

    class MessageThatDoesntStartTheSaga : IMessage
    {
        public int SomeProperty { get; set; }
    }


    class MessageThatStartsTheSaga : IMessage
    {
        public int SomeProperty { get; set; }
    }
}