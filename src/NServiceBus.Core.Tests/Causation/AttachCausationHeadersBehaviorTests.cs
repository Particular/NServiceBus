namespace NServiceBus.Core.Tests.Causation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class AttachCausationHeadersBehaviorTests
    {
        [Test]
        public async Task Should_set_the_conversation_id_to_new_guid_when_not_sent_from_handler()
        {
            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            await behavior.Invoke(context, ()=> Task.FromResult(0));

            Assert.AreNotEqual(Guid.Empty.ToString(), context.Headers[Headers.ConversationId]);
        }
        
        [Test]
        public async Task Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            var transportMessage = new IncomingMessage("xyz", new Dictionary<string, string> { { Headers.ConversationId, incomingConversationId } }, Stream.Null);
            context.Set(transportMessage);

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(incomingConversationId, context.Headers[Headers.ConversationId]);
        }

        [Test,Ignore("Will be refactored to use a explicit override via options instead and not rely on the header being set")]
        public async Task Should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(userConversationId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            
            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            context.Set(new IncomingMessage("the message id", new Dictionary<string, string>(), Stream.Null));

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("the message id", context.Headers[Headers.RelatedTo]);
        }

        static OutgoingPhysicalMessageContext InitializeContext()
        {
            var context = new OutgoingPhysicalMessageContext(null, null, ContextHelpers.GetOutgoingContext(new SendOptions()));
            return context;
        }
    }
}