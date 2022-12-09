namespace NServiceBus.Core.Tests.Recoverability
{
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class DiscardRecoverabilityActionTests
    {
        [Test]
        public void Discard_action_should_discard_message()
        {
            var discardAction = new Discard("not needed anymore");
            var actionContext = new TestableRecoverabilityContext();

            var routingContexts = discardAction.GetRoutingContexts(actionContext);

            CollectionAssert.IsEmpty(routingContexts);
            Assert.AreEqual(discardAction.ErrorHandleResult, ErrorHandleResult.Handled);
        }
    }
}