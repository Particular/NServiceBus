namespace NServiceBus.Core.Tests.Recoverability;

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

        Assert.That(routingContexts, Is.Empty);
        Assert.That(discardAction.ErrorHandleResult, Is.EqualTo(ErrorHandleResult.Handled));
    }
}