namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    [Ignore]
    public class When_performing_slr_with_serialization_exception : When_performing_slr
    {
        [Test]
        public async Task Should_preserve_the_original_body_for_serialization_exceptions()
        {
            var context = await Scenario.Define<Context>(c => { c.SimulateSerializationException = true; })
                .WithEndpoint<RetryEndpoint>(b => b.Given(bus =>
                {
                    bus.SendLocal(new MessageToBeRetried());
                    return Task.FromResult(0);
                }))
                .AllowExceptions(e => e is SimulatedException)
                .Done(c => c.SlrChecksum != default(byte))
                .Run();

            Assert.AreEqual(context.OriginalBodyChecksum, context.SlrChecksum, "The body of the message sent to slr should be the same as the original message coming off the queue");
        }
    }
}