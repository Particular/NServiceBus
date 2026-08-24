namespace NServiceBus.Core.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Testing;
using NUnit.Framework;

[TestFixture]
public class TypedMessageInstanceOverloadsTests
{
    [Test]
    public async Task Send_object_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (object)new MyMessage();
        await session.Send(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.Send(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_interface_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (IMyMessage)new MyMessage();
        await session.Send(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_explicit_generic_call_via_IMessageSession_uses_generic_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.Send<MyMessage>(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(0));
        Assert.That(session.SendGenericCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Send_with_destination_object_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (object)new MyMessage();
        await session.Send("destination", message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_with_destination_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.Send("destination", message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SendLocal_object_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (object)new MyMessage();
        await session.SendLocal(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SendLocal_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.SendLocal(message);
        Assert.That(session.SendObjectCount, Is.EqualTo(1));
        Assert.That(session.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_object_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (object)new MyMessage();
        await session.Publish(message);
        Assert.That(session.PublishObjectCount, Is.EqualTo(1));
        Assert.That(session.PublishGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.Publish(message);
        Assert.That(session.PublishObjectCount, Is.EqualTo(1));
        Assert.That(session.PublishGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_interface_typed_variable_via_IMessageSession_uses_object_overload()
    {
        var session = new TrackingMessageSession();
        var message = (IMyMessage)new MyMessage();
        await session.Publish(message);
        Assert.That(session.PublishObjectCount, Is.EqualTo(1));
        Assert.That(session.PublishGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_explicit_generic_call_via_IMessageSession_uses_generic_overload()
    {
        var session = new TrackingMessageSession();
        var message = new MyMessage();
        await session.Publish<MyMessage>(message);
        Assert.That(session.PublishObjectCount, Is.EqualTo(0));
        Assert.That(session.PublishGenericCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Reply_object_variable_via_IMessageProcessingContext_uses_object_overload()
    {
        var context = new TrackingMessageProcessingContext();
        var message = (object)new MyMessage();
        await context.Reply(message);
        Assert.That(context.ReplyObjectCount, Is.EqualTo(1));
        Assert.That(context.ReplyGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Reply_typed_variable_via_IMessageProcessingContext_uses_object_overload()
    {
        var context = new TrackingMessageProcessingContext();
        var message = new MyMessage();
        await context.Reply(message);
        Assert.That(context.ReplyObjectCount, Is.EqualTo(1));
        Assert.That(context.ReplyGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Reply_interface_typed_variable_via_IMessageProcessingContext_uses_object_overload()
    {
        var context = new TrackingMessageProcessingContext();
        var message = (IMyMessage)new MyMessage();
        await context.Reply(message);
        Assert.That(context.ReplyObjectCount, Is.EqualTo(1));
        Assert.That(context.ReplyGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Reply_explicit_generic_call_via_IMessageProcessingContext_uses_generic_overload()
    {
        var context = new TrackingMessageProcessingContext();
        var message = new MyMessage();
        await context.Reply<MyMessage>(message);
        Assert.That(context.ReplyObjectCount, Is.EqualTo(0));
        Assert.That(context.ReplyGenericCount, Is.EqualTo(1));
    }

    [Test]
    public async Task PipelineContext_Send_object_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = (object)new MyMessage();
        await context.Send(message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_typed_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.Send(message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_explicit_generic_call_via_IPipelineContext_uses_generic_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.Send<MyMessage>(message);
        Assert.That(context.SendObjectCount, Is.EqualTo(0));
        Assert.That(context.SendGenericCount, Is.EqualTo(1));
    }

    [Test]
    public async Task PipelineContext_Publish_object_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = (object)new MyMessage();
        await context.Publish(message);
        Assert.That(context.PublishObjectCount, Is.EqualTo(1));
        Assert.That(context.PublishGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Publish_typed_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.Publish(message);
        Assert.That(context.PublishObjectCount, Is.EqualTo(1));
        Assert.That(context.PublishGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Publish_explicit_generic_call_via_IPipelineContext_uses_generic_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.Publish<MyMessage>(message);
        Assert.That(context.PublishObjectCount, Is.EqualTo(0));
        Assert.That(context.PublishGenericCount, Is.EqualTo(1));
    }

    [Test]
    public async Task PipelineContext_SendLocal_object_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = (object)new MyMessage();
        await context.SendLocal(message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_SendLocal_typed_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.SendLocal(message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_with_destination_object_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = (object)new MyMessage();
        await context.Send("destination", message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_with_destination_typed_variable_via_IPipelineContext_uses_object_overload()
    {
        var context = new TrackingPipelineContext();
        var message = new MyMessage();
        await context.Send("destination", message);
        Assert.That(context.SendObjectCount, Is.EqualTo(1));
        Assert.That(context.SendGenericCount, Is.EqualTo(0));
    }

    [Test]
    public void Testable_outgoing_context_ordinary_call_uses_runtime_type()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        var message = (IMyMessage)new MyMessage();

        context.UpdateMessage(message);

        Assert.That(context.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public void Testable_outgoing_context_explicit_generic_call_uses_specified_type()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        var message = new MyMessage();

        context.UpdateMessage<IMyMessage>(message);

        Assert.That(context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
    }

    [Test]
    public void Testable_outgoing_context_explicit_type_validates_declared_type()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        object message = new MyMessage();

        var ex = Assert.Throws<ArgumentException>(() => context.UpdateMessage(message, typeof(string)));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void Testable_outgoing_context_explicit_type_rejects_null_instance()
    {
        var context = new TestableOutgoingLogicalMessageContext();

        Assert.Throws<ArgumentNullException>(() => context.UpdateMessage(null!, typeof(IMyMessage)));
    }

    [Test]
    public void Testable_outgoing_context_explicit_type_rejects_null_message_type()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        var message = new MyMessage();

        Assert.Throws<ArgumentNullException>(() => context.UpdateMessage(message, null!));
    }

    [Test]
    public void Testable_outgoing_context_explicit_type_preserves_declared_type_and_instance()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        object message = new MyMessage();

        context.UpdateMessage(message, typeof(IMyMessage));

        Assert.That(context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        Assert.That(context.Message.Instance, Is.SameAs(message));
    }

    [Test]
    public async Task Default_interface_fallback_Send_uses_object_overload()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        var message = new MyMessage();
        await session.Send<MyMessage>(message, new SendOptions());
        Assert.That(legacy.SendObjectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Default_interface_fallback_Publish_uses_object_overload()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        var message = new MyMessage();
        await session.Publish<MyMessage>(message, new PublishOptions());
        Assert.That(legacy.PublishObjectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Default_interface_fallback_Reply_uses_object_overload()
    {
        var legacy = new LegacyMessageProcessingContext();
        IMessageProcessingContext context = legacy;
        var message = new MyMessage();
        await context.Reply<MyMessage>(message, new ReplyOptions());
        Assert.That(legacy.ReplyObjectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Send_explicit_type_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.Send(message, typeof(IMyMessage));
        Assert.That(session.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_explicit_type_with_options_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.Send(message, typeof(IMyMessage), new SendOptions());
        Assert.That(session.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Send_with_destination_explicit_type_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.Send("destination", message, typeof(IMyMessage));
        Assert.That(session.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SendLocal_explicit_type_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.SendLocal(message, typeof(IMyMessage));
        Assert.That(session.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_explicit_type_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.Publish(message, typeof(IMyMessage));
        Assert.That(session.PublishExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.PublishObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Publish_explicit_type_with_options_via_IMessageSession_uses_explicit_type_overload()
    {
        var session = new TrackingMessageSession();
        object message = new MyMessage();
        await session.Publish(message, typeof(IMyMessage), new PublishOptions());
        Assert.That(session.PublishExplicitTypeCount, Is.EqualTo(1));
        Assert.That(session.PublishObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Reply_explicit_type_via_IMessageProcessingContext_uses_explicit_type_overload()
    {
        var context = new TrackingMessageProcessingContext();
        object message = new MyMessage();
        await context.Reply(message, typeof(IMyMessage));
        Assert.That(context.ReplyExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.ReplyObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Reply_explicit_type_with_options_via_IMessageProcessingContext_uses_explicit_type_overload()
    {
        var context = new TrackingMessageProcessingContext();
        object message = new MyMessage();
        await context.Reply(message, typeof(IMyMessage), new ReplyOptions());
        Assert.That(context.ReplyExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.ReplyObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_explicit_type_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.Send(message, typeof(IMyMessage));
        Assert.That(context.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_explicit_type_with_options_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.Send(message, typeof(IMyMessage), new SendOptions());
        Assert.That(context.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Send_with_destination_explicit_type_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.Send("destination", message, typeof(IMyMessage));
        Assert.That(context.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_SendLocal_explicit_type_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.SendLocal(message, typeof(IMyMessage));
        Assert.That(context.SendExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.SendObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Publish_explicit_type_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.Publish(message, typeof(IMyMessage));
        Assert.That(context.PublishExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.PublishObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task PipelineContext_Publish_explicit_type_with_options_via_IPipelineContext_uses_explicit_type_overload()
    {
        var context = new TrackingPipelineContext();
        object message = new MyMessage();
        await context.Publish(message, typeof(IMyMessage), new PublishOptions());
        Assert.That(context.PublishExplicitTypeCount, Is.EqualTo(1));
        Assert.That(context.PublishObjectCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Default_interface_fallback_Send_explicit_type_uses_object_overload()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        object message = new MyMessage();
        await session.Send(message, typeof(IMyMessage), new SendOptions());
        Assert.That(legacy.SendObjectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Default_interface_fallback_Publish_explicit_type_uses_object_overload()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        object message = new MyMessage();
        await session.Publish(message, typeof(IMyMessage), new PublishOptions());
        Assert.That(legacy.PublishObjectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Default_interface_fallback_Reply_explicit_type_uses_object_overload()
    {
        var legacy = new LegacyMessageProcessingContext();
        IMessageProcessingContext context = legacy;
        object message = new MyMessage();
        await context.Reply(message, typeof(IMyMessage), new ReplyOptions());
        Assert.That(legacy.ReplyObjectCount, Is.EqualTo(1));
    }

    [Test]
    public void Send_explicit_type_with_unrelated_type_throws()
    {
        var session = new TestableMessageSession();
        object message = new MyMessage();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.Send(message, typeof(MyOtherMessage), new SendOptions()));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void Send_explicit_type_with_null_type_throws()
    {
        var session = new TestableMessageSession();
        object message = new MyMessage();
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.Send(message, null!, new SendOptions()));
    }

    [Test]
    public void Send_explicit_type_with_null_message_throws()
    {
        var session = new TestableMessageSession();
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.Send(null!, typeof(IMyMessage), new SendOptions()));
    }

    [Test]
    public void Default_interface_fallback_Send_explicit_type_with_null_message_throws()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.Send(null!, typeof(IMyMessage), new SendOptions()));
    }

    [Test]
    public void Publish_explicit_type_with_unrelated_type_throws()
    {
        var session = new TestableMessageSession();
        object message = new MyMessage();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.Publish(message, typeof(MyOtherMessage), new PublishOptions()));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void Reply_explicit_type_with_unrelated_type_throws()
    {
        var context = new TestableMessageProcessingContext();
        object message = new MyMessage();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await context.Reply(message, typeof(MyOtherMessage), new ReplyOptions()));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void PipelineContext_Send_explicit_type_with_unrelated_type_throws()
    {
        var context = new TestablePipelineContext();
        object message = new MyMessage();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await context.Send(message, typeof(MyOtherMessage), new SendOptions()));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void PipelineContext_Publish_explicit_type_with_unrelated_type_throws()
    {
        var context = new TestablePipelineContext();
        object message = new MyMessage();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await context.Publish(message, typeof(MyOtherMessage), new PublishOptions()));
        Assert.That(ex!.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void Default_interface_fallback_Send_explicit_type_with_unrelated_type_throws()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        object message = new MyMessage();
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.Send(message, typeof(MyOtherMessage), new SendOptions()));
    }

    [Test]
    public void Default_interface_fallback_Publish_explicit_type_with_unrelated_type_throws()
    {
        var legacy = new LegacyMessageSession();
        IMessageSession session = legacy;
        object message = new MyMessage();
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.Publish(message, typeof(MyOtherMessage), new PublishOptions()));
    }

    [Test]
    public void Default_interface_fallback_Reply_explicit_type_with_unrelated_type_throws()
    {
        var legacy = new LegacyMessageProcessingContext();
        IMessageProcessingContext context = legacy;
        object message = new MyMessage();
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await context.Reply(message, typeof(MyOtherMessage), new ReplyOptions()));
    }

    public interface IMyMessage
    {
    }

    public class MyMessage : IMyMessage
    {
    }

    public class MyOtherMessage
    {
    }

    class TrackingMessageSession : TestableMessageSession
    {
        public int SendObjectCount;
        public int SendGenericCount;
        public int SendExplicitTypeCount;
        public int PublishObjectCount;
        public int PublishGenericCount;
        public int PublishExplicitTypeCount;

        public override Task Send(object message, SendOptions options, CancellationToken cancellationToken = default)
        {
            if (!trackingGenericSend && !trackingExplicitTypeSend)
            {
                SendObjectCount++;
            }

            return base.Send(message, options, cancellationToken);
        }

        public override Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions options, CancellationToken cancellationToken = default)
        {
            SendGenericCount++;
            trackingGenericSend = true;
            try
            {
                return base.Send<T>(message, options, cancellationToken);
            }
            finally
            {
                trackingGenericSend = false;
            }
        }

        public override Task Send(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions options, CancellationToken cancellationToken = default)
        {
            SendExplicitTypeCount++;
            trackingExplicitTypeSend = true;
            try
            {
                return base.Send(message, messageType, options, cancellationToken);
            }
            finally
            {
                trackingExplicitTypeSend = false;
            }
        }

        public override Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default)
        {
            if (!trackingGenericPublish && !trackingExplicitTypePublish)
            {
                PublishObjectCount++;
            }

            return base.Publish(message, options, cancellationToken);
        }

        public override Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
        {
            PublishGenericCount++;
            trackingGenericPublish = true;
            try
            {
                return base.Publish<T>(message, options, cancellationToken);
            }
            finally
            {
                trackingGenericPublish = false;
            }
        }

        public override Task Publish(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions options, CancellationToken cancellationToken = default)
        {
            PublishExplicitTypeCount++;
            trackingExplicitTypePublish = true;
            try
            {
                return base.Publish(message, messageType, options, cancellationToken);
            }
            finally
            {
                trackingExplicitTypePublish = false;
            }
        }

        bool trackingGenericSend;
        bool trackingGenericPublish;
        bool trackingExplicitTypeSend;
        bool trackingExplicitTypePublish;
    }

    class TrackingPipelineContext : TestablePipelineContext
    {
        public int SendObjectCount;
        public int SendGenericCount;
        public int SendExplicitTypeCount;
        public int PublishObjectCount;
        public int PublishGenericCount;
        public int PublishExplicitTypeCount;

        public override Task Send(object message, SendOptions options)
        {
            if (!trackingGenericSend && !trackingExplicitTypeSend)
            {
                SendObjectCount++;
            }

            return base.Send(message, options);
        }

        public override Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions options)
        {
            SendGenericCount++;
            trackingGenericSend = true;
            try
            {
                return base.Send<T>(message, options);
            }
            finally
            {
                trackingGenericSend = false;
            }
        }

        public override Task Send(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions options)
        {
            SendExplicitTypeCount++;
            trackingExplicitTypeSend = true;
            try
            {
                return base.Send(message, messageType, options);
            }
            finally
            {
                trackingExplicitTypeSend = false;
            }
        }

        public override Task Publish(object message, PublishOptions options)
        {
            if (!trackingGenericPublish && !trackingExplicitTypePublish)
            {
                PublishObjectCount++;
            }

            return base.Publish(message, options);
        }

        public override Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions options)
        {
            PublishGenericCount++;
            trackingGenericPublish = true;
            try
            {
                return base.Publish<T>(message, options);
            }
            finally
            {
                trackingGenericPublish = false;
            }
        }

        public override Task Publish(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions options)
        {
            PublishExplicitTypeCount++;
            trackingExplicitTypePublish = true;
            try
            {
                return base.Publish(message, messageType, options);
            }
            finally
            {
                trackingExplicitTypePublish = false;
            }
        }

        bool trackingGenericSend;
        bool trackingGenericPublish;
        bool trackingExplicitTypeSend;
        bool trackingExplicitTypePublish;
    }

    class TrackingMessageProcessingContext : TestableMessageProcessingContext
    {
        public int ReplyObjectCount;
        public int ReplyGenericCount;
        public int ReplyExplicitTypeCount;

        public override Task Reply(object message, ReplyOptions options)
        {
            if (!trackingGenericReply && !trackingExplicitTypeReply)
            {
                ReplyObjectCount++;
            }

            return base.Reply(message, options);
        }

        public override Task Reply<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, ReplyOptions options)
        {
            ReplyGenericCount++;
            trackingGenericReply = true;
            try
            {
                return base.Reply<T>(message, options);
            }
            finally
            {
                trackingGenericReply = false;
            }
        }

        public override Task Reply(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, ReplyOptions options)
        {
            ReplyExplicitTypeCount++;
            trackingExplicitTypeReply = true;
            try
            {
                return base.Reply(message, messageType, options);
            }
            finally
            {
                trackingExplicitTypeReply = false;
            }
        }

        bool trackingGenericReply;
        bool trackingExplicitTypeReply;
    }

    class LegacyMessageSession : IMessageSession
    {
        public int SendObjectCount;
        public int PublishObjectCount;

        public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            SendObjectCount++;
            return Task.CompletedTask;
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            PublishObjectCount++;
            return Task.CompletedTask;
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    class LegacyMessageProcessingContext : IMessageProcessingContext
    {
        public int ReplyObjectCount;

        CancellationToken ICancellableContext.CancellationToken => default;
        public Extensibility.ContextBag Extensions { get; set; } = new Extensibility.ContextBag();

        public Task Reply(object message, ReplyOptions options)
        {
            ReplyObjectCount++;
            return Task.CompletedTask;
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options) => throw new NotImplementedException();

        public string MessageId => throw new NotImplementedException();
        public string ReplyToAddress => throw new NotImplementedException();
        public IReadOnlyDictionary<string, string> MessageHeaders => throw new NotImplementedException();
        public Task ForwardCurrentMessageTo(string destination) => throw new NotImplementedException();

        public Task Send(object message, SendOptions options) => throw new NotImplementedException();
        public Task Send<T>(Action<T> messageConstructor, SendOptions options) => throw new NotImplementedException();
        public Task Publish(object message, PublishOptions options) => throw new NotImplementedException();
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions) => throw new NotImplementedException();
    }
}
