namespace NServiceBus.Core.Tests.Causation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class AttachCausationHeadersBehaviorTests
    {
        [Test]
        public async Task Should_generate_new_conversation_id_when_sending_outside_of_handlers()
        {
            var generatedId = "some generated conversation id";
            var behavior = new AttachCausationHeadersBehavior((ConversationIdStrategyContext c, out string id) =>
            {
                id = generatedId;
                return true;
            });
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(generatedId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_default_to_combguid_id()
        {
            var behavior = new AttachCausationHeadersBehavior((ConversationIdStrategyContext c, out string id) =>
            {
                id = null;
                return false;
            });
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(Guid.TryParse(context.Headers[Headers.ConversationId], out var _));
        }

        [Test]
        public void Should_not_allow_null_or_empty_id()
        {
            var behavior = new AttachCausationHeadersBehavior((ConversationIdStrategyContext c, out string id) =>
            {
                id = "";
                return true;
            });
            var context = new TestableOutgoingLogicalMessageContext();

            Assert.ThrowsAsync<Exception>(async () => await behavior.Invoke(context, ctx => TaskEx.CompletedTask));
        }

        [Test]
        public async Task Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior(NoOpStrategy);
            var context = new TestableOutgoingLogicalMessageContext();

            var transportMessage = new IncomingMessage("xyz", new Dictionary<string, string>
            {
                {Headers.ConversationId, incomingConversationId}
            }, new byte[0]);
            context.Extensions.Set(transportMessage);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(incomingConversationId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_include_outgoing_message_in_context()
        {
            ConversationIdStrategyContext contextProvided = null;

            var behavior = new AttachCausationHeadersBehavior((ConversationIdStrategyContext c, out string id) =>
            {
                contextProvided = c;
                id = null;
                return false;
            });

            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.NotNull(contextProvided);
            Assert.AreSame(contextProvided.Message, context.Message);
        }

        [Test]
        public async Task When_no_incoming_message_should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior(NoOpStrategy);
            var context = new TestableOutgoingLogicalMessageContext
            {
                Headers =
                {
                    [Headers.ConversationId] = userConversationId
                }
            };

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(userConversationId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public void When_user_defined_conversation_id_would_overwrite_incoming_conversation_id_should_throw()
        {
            var incomingConversationId = Guid.NewGuid().ToString();
            var userDefinedConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior(NoOpStrategy);
            var context = new TestableOutgoingLogicalMessageContext
            {
                Headers =
                {
                    [Headers.ConversationId] = userDefinedConversationId
                }
            };
            var transportMessage = new IncomingMessage("xyz", new Dictionary<string, string>
            {
                {Headers.ConversationId, incomingConversationId}
            }, new byte[0]);
            context.Extensions.Set(transportMessage);

            var exception = Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, ctx => TaskEx.CompletedTask));

            Assert.AreEqual($"Cannot set the {Headers.ConversationId} header to '{userDefinedConversationId}' as it cannot override the incoming header value ('{incomingConversationId}').", exception.Message);
        }

        [Test]
        public async Task Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var behavior = new AttachCausationHeadersBehavior(NoOpStrategy);
            var context = new TestableOutgoingLogicalMessageContext();

            context.Extensions.Set(new IncomingMessage("the message id", new Dictionary<string, string>(), new byte[0]));

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("the message id", context.Headers[Headers.RelatedTo]);
        }

        bool NoOpStrategy(ConversationIdStrategyContext context, out string customId)
        {
            customId = null;
            return false;
        }
    }
}