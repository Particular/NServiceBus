namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Diagnostics.PerformanceData;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class SagaModelTests
    {
        SagaMetadataCollection GetModel(params Type[] types)
        {
            var sagaMetaModel = new SagaMetadataCollection();
            sagaMetaModel.Initialize(types.ToList(), new Conventions());
            return sagaMetaModel;
        }

        [Test]
        public void FindSagasByName()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.Find(typeof(MySaga));


            Assert.NotNull(metadata);
        }

        [Test]
        public void FindSagasByEntityName()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.FindByEntity(typeof(MyEntity));


            Assert.NotNull(metadata);
        }

        [Test]
        public void ValidateAssumptionsAboutSagaMappings()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.Find(typeof(MySaga));

            Assert.NotNull(metadata);

            Assert.AreEqual(typeof(MyEntity), metadata.SagaEntityType);
            Assert.AreEqual(typeof(MyEntity).FullName, metadata.EntityName);
            Assert.AreEqual(typeof(MySaga), metadata.SagaType);
            Assert.AreEqual(typeof(MySaga).FullName, metadata.Name);

            Assert.AreEqual(2, metadata.AssociatedMessages.Count);
            Assert.AreEqual(1, metadata.AssociatedMessages.Count(am => am.MessageTypeName == typeof(Message1).FullName && am.IsAllowedToStartSaga));
            Assert.AreEqual(1, metadata.AssociatedMessages.Count(am => am.MessageTypeName == typeof(Message2).FullName && !am.IsAllowedToStartSaga));

            Assert.True(metadata.TryGetCorrelationProperty(out var correlatedProperty));
            Assert.AreEqual("UniqueProperty", correlatedProperty.Name);

            Assert.AreEqual(2, metadata.Finders.Count);
            Assert.AreEqual(1, metadata.Finders.Count(f => f.MessageType == typeof(Message1)));
            Assert.AreEqual(1, metadata.Finders.Count(f => f.MessageType == typeof(Message2)));
        }

        [Test]
        public void FilterOutNonSagaTypes()
        {
            var model = GetModel(typeof(MySaga), typeof(string), typeof(AbstractSaga)).ToList();

            Assert.That(model, Has.Exactly(1).Matches<SagaMetadata>(x => x.SagaType == typeof(MySaga)));
        }

        [Test]
        public void ValidateHeaderBasedSagaMappingsSetCorrelationProperty()
        {
            var model = GetModel(typeof(MyHeaderMappedSaga));

            var metadata = model.Find(typeof(MyHeaderMappedSaga));

            Assert.That(metadata.TryGetCorrelationProperty(out var correlatedProperty), Is.True);
            Assert.That(correlatedProperty.Name, Is.EqualTo("UniqueProperty"));
        }

        [Test]
        public void ValidateIfSagaEntityIsShared()
        {
            var model = GetModel(typeof(MySaga), typeof(MySaga2));

            var ex = Assert.Throws<Exception>(() => model.VerifyIfEntitiesAreShared());

            const string expectedExceptionMessage = "Best practice violation: Multiple saga types are sharing the same saga state which can result in persisters to physically share the same storage structure.\n\n- Entity 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaModelTests+MyEntity' used by saga types 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaModelTests+MySaga' and 'NServiceBus.Core.Tests.Sagas.TypeBasedSagas.SagaModelTests+MySaga2'.";

            Assert.AreEqual(expectedExceptionMessage, ex.Message);
        }

        class MyHeaderMappedSaga : Saga<MyHeaderMappedSaga.SagaData>, IAmStartedByMessages<Message1>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureHeaderMapping<Message1>("CorrelationHeader")
                    .ToSaga(saga => saga.UniqueProperty);
            }

            public class SagaData : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }
        }

        class MySaga : Saga<MyEntity>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public Task Handle(Message2 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<Message1>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
                mapper.ConfigureMapping<Message2>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
            }
        }
        public class MyEntity : ContainSagaData
        {
            public int UniqueProperty { get; set; }
        }

        public class MyEntity2 : MyEntity
        {

        }

        class MySaga2 : Saga<MyEntity>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
        {
            public Task Handle(Message1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            public Task Handle(Message2 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<Message1>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
                mapper.ConfigureMapping<Message2>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
            }
        }

        abstract class AbstractSaga : Saga<MyEntity>, IAmStartedByMessages<Message1>
        {
            public abstract Task Handle(Message1 message, IMessageHandlerContext context);

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
            }
        }

        class Message1 : IMessage
        {
            public int UniqueProperty { get; set; }
        }

        class Message2 : IMessage
        {
            public int UniqueProperty { get; set; }
        }
    }
}