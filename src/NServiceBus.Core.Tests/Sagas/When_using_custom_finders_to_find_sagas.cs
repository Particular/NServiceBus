namespace NServiceBus.Unicast.Tests
{
    using System.Linq;
    using NUnit.Framework;
    using Saga;


    [TestFixture]
    class When_using_custom_finders_to_find_sagas : with_sagas
    {
        [Test]
        public void Should_use_the_most_specific_finder_first_when_receiving_a_message()
        {
            RegisterSaga<SagaWithDerivedMessage>();
            RegisterCustomFinder<MyFinderForBaseClass>();
            RegisterCustomFinder<MyFinderForFoo2>();
          
            ReceiveMessage(new Foo2());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            var sagaData = (MySagaData) persister.CurrentSagaEntities.First().Value.SagaEntity;

            Assert.AreEqual(typeof(MyFinderForFoo2).FullName,sagaData.SourceFinder);

        }

        [Test]
        public void Should_use_base_class_finder_if_needed()
        {
            RegisterSaga<SagaWithDerivedMessage>();
            RegisterCustomFinder<MyFinderForBaseClass>();

            ReceiveMessage(new Foo2());

            Assert.AreEqual(1, persister.CurrentSagaEntities.Count(), "Existing saga should be found");

            var sagaData = (MySagaData)persister.CurrentSagaEntities.First().Value.SagaEntity;

            Assert.AreEqual(typeof(MyFinderForBaseClass).FullName, sagaData.SourceFinder);

        }

        class SagaWithDerivedMessage : Saga<MySagaData>, IHandleMessages<Foo2>
        {
            public void Handle(Foo2 message)
            {
            }
        }
        class MySagaData : ContainSagaData
        {
            public string SourceFinder { get; set; }
        }

        class Foo2 : Foo
        {
        }
        abstract class Foo : IMessage
        {
        }

        class MyFinderForBaseClass : IFindSagas<MySagaData>.Using<Foo>
        {
            public MySagaData FindBy(Foo message)
            {
                return new MySagaData { SourceFinder = typeof(MyFinderForBaseClass).FullName };
            }
        }

        class MyFinderForFoo2 : IFindSagas<MySagaData>.Using<Foo2>
        {
            public MySagaData FindBy(Foo2 message)
            {
                return new MySagaData { SourceFinder = typeof(MyFinderForFoo2).FullName };
            }
        }
    }
}

