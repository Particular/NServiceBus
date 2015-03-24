namespace NServiceBus.AcceptanceTests.Retries
{
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
    }
}