namespace NServiceBus.TransportTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_using_unicode_characters_in_headers : NServiceBusTransportTest
    {
        [Test]
        public async Task Should_support_unicode_characters()
        {
            MessageContext messageContext = null;

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    messageContext = context;
                    return Task.CompletedTask;
                },
                (_, __) => Task.FromResult(ReceiveResult.Discarded),
                (_, __) => completed.SetCompleted(),
                TransportTransactionMode.None);

            var sentHeaders = new Dictionary<string, string>
            {
                { "a-B1", "a-B" },
                { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
                { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
                { "a-B4", "a-\U0001F60D-b" },
                { "a-\U0001F605-B5", "a-B" },
                { "a-B6", "a-😍-b" },
                { "a-😅-B7", "a-B" },
            };

            await SendMessage(InputQueueName, sentHeaders);

            _ = await completed.Task;

            Assert.IsNotEmpty(messageContext.Headers);
            CollectionAssert.IsSupersetOf(messageContext.Headers, sentHeaders);
        }
    }
}
