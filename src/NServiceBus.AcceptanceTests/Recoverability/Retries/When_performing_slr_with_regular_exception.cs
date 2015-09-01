namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_performing_slr_with_regular_exception : When_performing_slr
    {
        [Test]
        public void Should_preserve_the_original_body_for_regular_exceptions()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                .AllowExceptions()
                .Done(c => c.SlrChecksum != default(byte))
                .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.SlrChecksum, "The body of the message sent to slr should be the same as the original message coming off the queue");
        }

        [Test]
        public void Should_reschedule_message_three_times_by_default()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                .AllowExceptions()
                .Done(c => c.ForwardedToErrorQueue)
                .Run();

            Assert.IsTrue(context.ForwardedToErrorQueue);
            // we can't assert on the message id since the rescheduled message will have a new id each time
            Assert.AreEqual(3, context.Logs.Count(l => l.Message
                .StartsWith(string.Format("Second Level Retry will reschedule message message '{0}'", context.PhysicalMessageId))));
            Assert.AreEqual(1, context.Logs.Count(l => l.Message
                .StartsWith(string.Format("Giving up Second Level Retries for message '{0}'.", context.PhysicalMessageId))));
        }
    }
}