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
            var messageProcessed = CreateTaskCompletionSource<MessageContext>();

            await StartPump(
                (context, _) => messageProcessed.SetCompleted(context),
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                TransportTransactionMode.None);

            var headers = new Dictionary<string, string>
            {
                { "a-B1", "a-B" },
                { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
                { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
                { "a-B4", "a-\U0001F60D-b" },
                { "a-\U0001F605-B5", "a-B" },
                { "a-B6", "a-😍-b" },
                { "a-😅-B7", "a-B" },
            };

            await SendMessage(InputQueueName, headers);

            var messageContext = await messageProcessed.Task;

            Assert.IsNotEmpty(messageContext.Headers);
            CollectionAssert.IsSupersetOf(messageContext.Headers, headers);
        }
    }
}
