namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class BehaviorListTests
    {
        [Test]
        public void Replace()
        {
            var behaviorList = new BehaviorList<FakeContext>
                {
                    InnerList = new List<Type>
                        {
                            typeof(Behavior1)
                        }
                };
            behaviorList.Replace<Behavior1, Behavior2>();
            Assert.AreEqual(1, behaviorList.InnerList.Count);
            Assert.AreEqual(typeof(Behavior2), behaviorList.InnerList.First());
        }

        [Test]
        public void Add()
        {
            var behaviorList = new BehaviorList<FakeContext>
                {
                    InnerList = new List<Type>
                        {
                            typeof(Behavior1)
                        }
                };
            behaviorList.Add<Behavior2>();
            Assert.AreEqual(2, behaviorList.InnerList.Count);
            Assert.AreEqual(typeof(Behavior2), behaviorList.InnerList[1]);
        }

        [Test]
        public void InsertAfter()
        {
            var behaviorList = new BehaviorList<FakeContext>
                {
                    InnerList = new List<Type>
                        {
                            typeof(Behavior1),
                            typeof(Behavior2),
                        }
                };
            behaviorList.InsertAfter<Behavior1,Behavior3>();
            Assert.AreEqual(3, behaviorList.InnerList.Count);
            Assert.AreEqual(typeof(Behavior3), behaviorList.InnerList[1]);
        }

        [Test]
        public void When_InsertAfter_and_target_does_not_exists_should_throw()
        {
            var behaviorList = new BehaviorList<FakeContext>();
            var exception = Assert.Throws<Exception>(behaviorList.InsertAfter<Behavior1, Behavior2>);
            Assert.AreEqual("Could not InsertAfter since 'Behavior1' does not exist.", exception.Message);
        }

        [Test]
        public void Remove()
        {
            var behaviorList = new BehaviorList<FakeContext>
                {
                    InnerList = new List<Type>
                        {
                            typeof(Behavior1),
                            typeof(Behavior2),
                        }
                };
            Assert.IsTrue(behaviorList.Remove<Behavior2>());
            Assert.AreEqual(1, behaviorList.InnerList.Count);
            Assert.AreEqual(typeof(Behavior1), behaviorList.InnerList[0]);
        }

        [Test]
        public void When_replacing_and_target_does_not_exists_should_throw()
        {
            var behaviorList = new BehaviorList<FakeContext>();
            var exception = Assert.Throws<Exception>(behaviorList.Replace<Behavior1, Behavior2>);
            Assert.AreEqual("Could not replace since 'Behavior1' does not exist.", exception.Message);
        }

        [Test]
        public void InsertBefore()
        {
            var behaviorList = new BehaviorList<FakeContext>
                {
                    InnerList = new List<Type>
                        {
                            typeof(Behavior1),
                            typeof(Behavior2),
                        }
                };
            behaviorList.InsertBefore<Behavior2,Behavior3>();
            Assert.AreEqual(3, behaviorList.InnerList.Count);
            Assert.AreEqual(typeof(Behavior3), behaviorList.InnerList[1]);
        }

        [Test]
        public void When_InsertBefore_and_target_does_not_exists_should_throw()
        {
            var behaviorList = new BehaviorList<FakeContext>();
            var exception = Assert.Throws<Exception>(behaviorList.InsertBefore<Behavior1, Behavior2>);
            Assert.AreEqual("Could not InsertBefore  since 'Behavior1' does not exist.", exception.Message);
        }

        class Behavior1 : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
            }
        }

        class Behavior2 : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
            }
        }

        class Behavior3 : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
            }
        }

        class FakeContext : BehaviorContext
        {
            public FakeContext(BehaviorContext parentContext) : base(parentContext)
            {
            }
        }
    }
}
