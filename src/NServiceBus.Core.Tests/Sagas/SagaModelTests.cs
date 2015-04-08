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
            var sagaMetadatas = TypeBasedSagaMetaModel.Create(types.ToList(),new Conventions());
            var sagaMetaModel = new SagaMetaModel();
            sagaMetaModel.Initialize(sagaMetadatas);
            return sagaMetaModel;
        }

        [Test]
        public void FindSagasByName()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.FindByName(typeof(MySaga).FullName);


            Assert.NotNull(metadata);
        }

        [Test]
        public void FindSagasByEntityName()
        {
            var model = GetModel(typeof(MySaga));

            var metadata = model.FindByEntityName(typeof(MySaga.MyEntity).FullName);


            Assert.NotNull(metadata);
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