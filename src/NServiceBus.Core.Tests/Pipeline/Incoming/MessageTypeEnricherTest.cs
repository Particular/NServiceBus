namespace NServiceBus.Core.Tests.Pipeline.Incoming
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageTypeEnricherTest
    {
        [Test]
        public async Task When_processing_message_without_enclosed_message_type_header_it_is_addedAsync()
        {
            var behavior = new InferredMessageTypeEnricherBehavior();
            var context = new TestableIncomingLogicalMessageContext();

            Assert.IsFalse(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));

            await behavior.Invoke(context, messageContext => Task.CompletedTask);

            Assert.IsTrue(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));
            Assert.AreEqual(context.Headers[Headers.EnclosedMessageTypes], typeof(object).FullName);
        }

        [Test]
        public async Task When_processing_message_with_enclosed_message_type_header_it_is_not_changedAsync()
        {
            var mutator = new InferredMessageTypeEnricherBehavior();
            var context = new TestableIncomingLogicalMessageContext();
            context.Headers.Add(Headers.EnclosedMessageTypes, typeof(string).FullName);

            await mutator.Invoke(context, messageContext => Task.CompletedTask);

            Assert.IsTrue(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));
            Assert.AreEqual(context.Headers[Headers.EnclosedMessageTypes], typeof(string).FullName);

        }
    }
}