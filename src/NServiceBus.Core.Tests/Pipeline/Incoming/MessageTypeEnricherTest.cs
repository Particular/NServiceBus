namespace NServiceBus.Core.Tests.Pipeline.Incoming
{
    using NServiceBus;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageTypeEnricherTest
    {
        [Test]
        public void When_processing_message_without_enclosed_message_type_header_it_is_added()
        {
            var behavior = new InferredMessageTypeEnricherBehavior();
            var context = new TestableIncomingLogicalMessageContext();

            Assert.IsFalse(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));

            behavior.Invoke(context, messageContext => TaskEx.CompletedTask);

            Assert.IsTrue(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));
            Assert.AreEqual(context.Headers[Headers.EnclosedMessageTypes], typeof(object).FullName);
        }

        [Test]
        public void When_processing_message_with_enclosed_message_type_header_it_is_not_changed()
        {
            var mutator = new InferredMessageTypeEnricherBehavior();
            var context = new TestableIncomingLogicalMessageContext();
            context.Headers.Add(Headers.EnclosedMessageTypes, typeof(string).FullName);

            mutator.Invoke(context, messageContext => TaskEx.CompletedTask);

            Assert.IsTrue(context.Headers.ContainsKey(Headers.EnclosedMessageTypes));
            Assert.AreEqual(context.Headers[Headers.EnclosedMessageTypes], typeof(string).FullName);

        }
    }
}