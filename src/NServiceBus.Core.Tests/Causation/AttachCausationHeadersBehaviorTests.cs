﻿namespace NServiceBus.Core.Tests.Causation
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
        public async Task Should_set_the_conversation_id_to_new_guid_when_not_sent_from_handler()
        {
            var behavior = new AttachCausationHeadersBehavior();
            var context = new TestableOutgoingPhysicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreNotEqual(Guid.Empty.ToString(), context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = new TestableOutgoingPhysicalMessageContext();

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

            var behavior = new AttachCausationHeadersBehavior();
            var context = new TestableOutgoingPhysicalMessageContext
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
        public async Task When_incoming_message_should_override_a_conversation_id_specified_by_the_user()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = new TestableOutgoingPhysicalMessageContext
            {
                Headers =
                {
                    [Headers.ConversationId] = Guid.NewGuid().ToString()
                }
            };
            var transportMessage = new IncomingMessage("xyz", new Dictionary<string, string>
            {
                {Headers.ConversationId, incomingConversationId}
            }, new byte[0]);
            context.Extensions.Set(transportMessage);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(incomingConversationId, context.Headers[Headers.ConversationId]);
        }

        [Test]
        public async Task Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var behavior = new AttachCausationHeadersBehavior();
            var context = new TestableOutgoingPhysicalMessageContext();

            context.Extensions.Set(new IncomingMessage("the message id", new Dictionary<string, string>(), new byte[0]));

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("the message id", context.Headers[Headers.RelatedTo]);
        }
    }
}