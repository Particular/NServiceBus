namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Linq;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class SagaModelTests
    {

        SagaMetaModel GetModel(params Type[] types)
        {
            var sagaMetaModel = new SagaMetaModel();
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

            var metadata = model.FindByEntity(typeof(MySaga.MyEntity));


            Assert.NotNull(metadata);
        }

        [Test]
        public void ValidateAssumptionsAboutSagaMappings()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.Find(typeof(MySaga));

            Assert.NotNull(metadata);

            Assert.AreEqual(typeof(MySaga.MyEntity), metadata.SagaEntityType);
            Assert.AreEqual(typeof(MySaga.MyEntity).FullName, metadata.EntityName);
            Assert.AreEqual(typeof(MySaga), metadata.SagaType);
            Assert.AreEqual(typeof(MySaga).FullName, metadata.Name);

            Assert.AreEqual(2, metadata.AssociatedMessages.Count());
            Assert.AreEqual(1, metadata.AssociatedMessages.Count(am => am.MessageType == typeof(Message1).FullName && am.IsAllowedToStartSaga == false));
            Assert.AreEqual(1, metadata.AssociatedMessages.Count(am => am.MessageType == typeof(Message2).FullName && am.IsAllowedToStartSaga == false));

            Assert.AreEqual(1, metadata.CorrelationProperties.Count);
            Assert.AreEqual("UniqueProperty", metadata.CorrelationProperties[0].Name);

            Assert.AreEqual(2, metadata.Finders.Count());
            Assert.AreEqual(1, metadata.Finders.Count(f => f.MessageType == typeof(Message1).FullName));
            Assert.AreEqual(1, metadata.Finders.Count(f => f.MessageType == typeof(Message2).FullName));
        }

        [Test]
        public void FilterOutNonSagaTypes()
        {
            var model = GetModel(typeof(MySaga),typeof(string));

            Assert.AreEqual(1, model.Count());
        }

        class MySaga : Saga<MySaga.MyEntity>,IHandleMessages<Message1>,IHandleMessages<Message2>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {
                mapper.ConfigureMapping<Message1>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
                mapper.ConfigureMapping<Message2>(m => m.UniqueProperty).ToSaga(s => s.UniqueProperty);
            }

            public class MyEntity : ContainSagaData
            {
                public int UniqueProperty { get; set; }
            }

            public void Handle(Message1 message)
            {
                throw new NotImplementedException();
            }

            public void Handle(Message2 message)
            {
                throw new NotImplementedException();
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