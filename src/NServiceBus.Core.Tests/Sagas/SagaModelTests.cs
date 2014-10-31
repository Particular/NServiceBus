namespace NServiceBus.Core.Tests.Sagas.TypeBasedSagas
{
    using System;
    using System.Linq;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class SagaModelTests
    {

        SagaMetaModel GetModel(params Type[] types)
        {
            return new SagaMetaModel(TypeBasedSagaMetaModel.Create(types.ToList(),new Conventions()));
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

        class MySaga : Saga<MySaga.MyEntity>,IHandleMessages<Message1>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyEntity> mapper)
            {

            }

            public class MyEntity : ContainSagaData
            {
                [Unique]
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

        class Message1 : IMessage { }
        class Message2 : IMessage { }


    }
}