namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    internal class SagasTests
    {
        [SetUp]
        public void Setup()
        {
            Configure.With(Enumerable.Empty<Type>())
                .DefaultBuilder();
        }

        internal class SagaWithNoHandlers : Saga<SagaWithNoHandlers.MySagaData>
        {
            internal class MySagaData : ContainSagaData
            {
            }
        }

        internal class SagaWithStarter : Saga<SagaWithStarter.MySagaData>, IAmStartedByMessages<SagaWithStarter.Foo>
        {
            public void Handle(Foo message)
            {
            }

            internal class Foo : IMessage
            {
            }

            internal class MySagaData : ContainSagaData
            {
            }
        }

        internal class SagaWithHandlers : Saga<SagaWithHandlers.MySagaData>, IHandleMessages<SagaWithHandlers.Foo>, IHandleMessages<SagaWithHandlers.Bar>
        {
            public void Handle(Bar message)
            {
            }

            public void Handle(Foo message)
            {
            }

            internal class Bar : IMessage
            {
            }

            internal class Foo : IMessage
            {
            }

            internal class MySagaData : ContainSagaData
            {
            }
        }

        internal class SagaWithDerivedMessage : Saga<SagaWithStarter.MySagaData>, IHandleMessages<SagaWithDerivedMessage.Foo2>
        {
            public void Handle(Foo2 message)
            {
            }

            internal class Foo2 : Foo
            {
            }
            internal abstract class Foo : IMessage
            {
            }

            internal class MySagaData : ContainSagaData
            {
            }
        }

        internal class MyFinderForBaseClass : IFindSagas<SagaWithDerivedMessage.MySagaData>.Using<SagaWithDerivedMessage.Foo>
        {
            public SagaWithDerivedMessage.MySagaData FindBy(SagaWithDerivedMessage.Foo message)
            {
                return new SagaWithDerivedMessage.MySagaData();
            }
        }

        internal class MyFinderForFoo2: IFindSagas<SagaWithDerivedMessage.MySagaData>.Using<SagaWithDerivedMessage.Foo2>
        {
            public SagaWithDerivedMessage.MySagaData FindBy(SagaWithDerivedMessage.Foo2 message)
            {
                return new SagaWithDerivedMessage.MySagaData();
            }
        }

        internal class MyFinder : IFindSagas<SagaWithStarter.MySagaData>.Using<SagaWithStarter.Foo>
        {
            public SagaWithStarter.MySagaData FindBy(SagaWithStarter.Foo message)
            {
                return new SagaWithStarter.MySagaData();
            }
        }

        [Test]
        public void FindAndConfigureSagasIn()
        {
            var sagas = new Sagas();

            var result = sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithNoHandlers)
            });

            Assert.True(result);
        }

        [Test]
        public void GetMessageTypesHandledBySaga_SagaWithHandlers()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithHandlers)
            });

            var types = Sagas.GetMessageTypesHandledBySaga(typeof(SagaWithHandlers));

            Assert.AreEqual(2, types.Count());
        }

        [Test]
        public void GetMessageTypesHandledBySaga_SagaWithNoHandlers()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithNoHandlers)
            });

            Assert.IsFalse(Sagas.GetMessageTypesHandledBySaga(typeof(SagaWithNoHandlers)).Any());
        }

        [Test]
        public void GetSagaTypeToStartIfMessageNotFoundByFinder()
        {
            var foo = new SagaWithStarter.Foo();
            IFinder finder = new MyFinder();

            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithStarter),
                typeof(MyFinder)
            });

            Assert.AreEqual(typeof(SagaWithStarter), Sagas.GetSagaTypeToStartIfMessageNotFoundByFinder(foo, finder));
        }

        [Test]
        public void ShouldMessageStartSaga()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithStarter)
            });

            Assert.IsTrue(Sagas.ShouldMessageStartSaga(typeof(SagaWithStarter), typeof(SagaWithStarter.Foo)));
            Assert.IsFalse(Sagas.ShouldMessageStartSaga(typeof(SagaWithStarter), typeof(SagaWithHandlers.Foo)));
        }

        [Test]
        public void GetSagaTypeForSagaEntityType()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithStarter),
            });

            Assert.AreEqual(typeof(SagaWithStarter), Sagas.GetSagaTypeForSagaEntityType(typeof(SagaWithStarter.MySagaData)));
        }

        [Test]
        public void GetSagaEntityTypeForSagaType()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithStarter),
            });

            Assert.AreEqual(typeof(SagaWithStarter.MySagaData), Sagas.GetSagaEntityTypeForSagaType(typeof(SagaWithStarter)));
        }

        [Test]
        public void GetFindByMethodForFinder()
        {
            var foo = new SagaWithStarter.Foo();
            IFinder finder = new MyFinder();

            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithStarter),
                typeof(MyFinder)
            });

            var expected = typeof(MyFinder).GetMethod("FindBy");
            Assert.AreEqual(expected, Sagas.GetFindByMethodForFinder(finder, foo));
        }

        [Test]
        public void GetFindByMethodForFinder_DerivedMessage()
        {
            var foo = new SagaWithDerivedMessage.Foo2();
            IFinder finder = new MyFinderForBaseClass();

            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithDerivedMessage),
                typeof(MyFinderForBaseClass)
            });

            var expected = typeof(MyFinderForBaseClass).GetMethod("FindBy");
            Assert.AreEqual(expected, Sagas.GetFindByMethodForFinder(finder, foo));
        }

        [Test]
        public void GetFindersForMessageAndEntity()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithDerivedMessage),
                typeof(MyFinderForBaseClass),
                typeof(MyFinderForFoo2)
            });

            var types = Sagas.GetFindersForMessageAndEntity(typeof(SagaWithDerivedMessage.Foo2), typeof(SagaWithDerivedMessage.MySagaData)).ToList();
            CollectionAssert.Contains(types, typeof(MyFinderForBaseClass));
            CollectionAssert.Contains(types, typeof(MyFinderForFoo2));
        }

        [Test]
        public void GetFindersFor()
        {
            var sagas = new Sagas();

            sagas.FindAndConfigureSagasIn(new[]
            {
                typeof(SagaWithDerivedMessage),
                typeof(MyFinderForBaseClass),
                typeof(MyFinderForFoo2)
            });

            var types = Sagas.GetFindersFor(new SagaWithDerivedMessage.Foo2()).ToList();
            CollectionAssert.Contains(types, typeof(MyFinderForBaseClass));
            CollectionAssert.Contains(types, typeof(MyFinderForFoo2));
        }
    }
}