﻿namespace NServiceBus.Core.Tests.Causation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
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
            var behavior = new AttachCausationHeadersBehavior(_ => generatedId);
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(generatedId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
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
        public async Task When_no_incoming_message_should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
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

            var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
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
            var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
            var context = new TestableOutgoingLogicalMessageContext();

            context.Extensions.Set(new IncomingMessage("the message id", new Dictionary<string, string>(), new byte[0]));

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("the message id", context.Headers[Headers.RelatedTo]);
        }

        string ReturnDefaultConversationId(IOutgoingLogicalMessageContext context)
        {
            return ConversationId.Default.Value;
        }
    }
}