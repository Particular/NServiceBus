namespace NServiceBus.Core.Tests.Causation;

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

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo(generatedId));
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
        }, Array.Empty<byte>());
        context.Extensions.Set(transportMessage);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo(incomingConversationId));
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

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo(userConversationId));
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
        }, Array.Empty<byte>());
        context.Extensions.Set(transportMessage);

        var exception = Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, ctx => Task.CompletedTask));

        Assert.That(exception.Message, Is.EqualTo($"Cannot set the {Headers.ConversationId} header to '{userDefinedConversationId}' as it cannot override the incoming header value ('{incomingConversationId}'). To start a new conversation use sendOptions.StartNewConversation()."));
    }

    [Test]
    public async Task Should_set_the_related_to_header_with_the_id_of_the_current_message()
    {
        var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
        var context = new TestableOutgoingLogicalMessageContext();

        context.Extensions.Set(new IncomingMessage("the message id", [], Array.Empty<byte>()));

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.RelatedTo], Is.EqualTo("the message id"));
    }

    [Test]
    public async Task Should_set_start_new_conversation_id_when_explicit_conversation_id_is_provided()
    {
        var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
        var options = new SendOptions();
        options.StartNewConversation("new conversation");

        var context = new TestableOutgoingLogicalMessageContext
        {
            Extensions = options.Context
        };

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo("new conversation"));
    }

    [Test]
    public async Task Should_set_start_new_conversation_id_using_strategy_when_no_explicit_conversation_id_is_provided()
    {
        var generatedId = "generated conversation";
        var behavior = new AttachCausationHeadersBehavior(_ => generatedId);
        var options = new SendOptions();
        options.StartNewConversation();

        var context = new TestableOutgoingLogicalMessageContext
        {
            Extensions = options.Context
        };

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo(generatedId));
    }

    [Test]
    public async Task Should_set_previous_conversation_id_when_starting_new_conversation_from_incoming_message()
    {
        var incomingConversationId = "incoming conversation";
        var behavior = new AttachCausationHeadersBehavior(_ => "new conversation");
        var options = new SendOptions();
        options.StartNewConversation();

        var context = new TestableOutgoingLogicalMessageContext
        {
            Extensions = options.Context
        };
        context.Extensions.Set(new IncomingMessage("message-id", new Dictionary<string, string>
        {
            { Headers.ConversationId, incomingConversationId }
        }, Array.Empty<byte>()));

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.ConversationId], Is.EqualTo("new conversation"));
            Assert.That(context.Headers[Headers.PreviousConversationId], Is.EqualTo(incomingConversationId));
        }
    }

    [Test]
    public void When_user_defined_conversation_id_is_set_and_start_new_conversation_was_requested_should_throw()
    {
        var behavior = new AttachCausationHeadersBehavior(ReturnDefaultConversationId);
        var options = new SendOptions();
        options.StartNewConversation();

        var context = new TestableOutgoingLogicalMessageContext
        {
            Extensions = options.Context,
            Headers =
            {
                [Headers.ConversationId] = "user-defined"
            }
        };

        var exception = Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, ctx => Task.CompletedTask));

        Assert.That(exception.Message, Is.EqualTo($"Cannot set the {Headers.ConversationId} header to 'user-defined' as StartNewConversation() was called."));
    }

    string ReturnDefaultConversationId(IOutgoingLogicalMessageContext context)
    {
        return ConversationId.Default.Value;
    }
}