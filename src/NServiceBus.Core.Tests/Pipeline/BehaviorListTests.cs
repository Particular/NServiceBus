namespace NServiceBus.Core.Tests.Sagas
{
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;

    [TestFixture]
    public class BehaviorListTests
    {

        [Test]
        public void Can_add_a_type()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();
            behaviorList.Add<InvokeHandlersBehavior>();
            Assert.IsTrue(behaviorList.InnerList.Contains(typeof(InvokeHandlersBehavior)));
        }

        [Test]
        public void Can_remove_a_type()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();
            behaviorList.Add<InvokeHandlersBehavior>();
            behaviorList.Remove<InvokeHandlersBehavior>();
            Assert.IsFalse(behaviorList.InnerList.Contains(typeof(InvokeHandlersBehavior)));
        }

        [Test]
        public void Can_replace_a_type()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();
            behaviorList.Add<InvokeHandlersBehavior>();
            behaviorList.Replace<InvokeHandlersBehavior, SagaPersistenceBehavior>();
            Assert.IsTrue(behaviorList.InnerList.Contains(typeof(SagaPersistenceBehavior)));
            Assert.AreEqual(1,behaviorList.InnerList.Count);
        }

        [Test]
        public void Can_insert_after_a_type()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();
            behaviorList.Add<InvokeHandlersBehavior>();
            behaviorList.InsertAfter<InvokeHandlersBehavior, SagaPersistenceBehavior>();
            Assert.AreEqual(typeof(InvokeHandlersBehavior), behaviorList.InnerList[0]);
            Assert.AreEqual(typeof(SagaPersistenceBehavior), behaviorList.InnerList[1]);
        }

        [Test]
        public void Can_insert_before_a_type()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();
            behaviorList.Add<InvokeHandlersBehavior>();
            behaviorList.InsertBefore<InvokeHandlersBehavior, SagaPersistenceBehavior>();
            Assert.AreEqual(typeof(SagaPersistenceBehavior), behaviorList.InnerList[0]);
            Assert.AreEqual(typeof(InvokeHandlersBehavior), behaviorList.InnerList[1]);
        }
    }
}