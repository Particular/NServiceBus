﻿namespace NServiceBus.Core.Tests.Pipeline.MutateInstanceMessage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using Transport;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    class MutateOutgoingMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null ));
            context.Extensions.Set(new LogicalMessage(null, null));
            context.Builder.Register<IMutateOutgoingMessages>(() => new MutateOutgoingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateOutgoingMessagesReturnsNull : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                return null;
            }
        }

        [Test]
        public void Show_thow_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_modifies_messageId_header()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>(), Stream.Null));
            context.Extensions.Set(new LogicalMessage(null, null));
            context.Builder.Register<IMutateOutgoingMessages>(() => new MutateOutgoingMessagesModifiesMessageIdHeader());

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Setting Message Id by manipulating the `NServiceBus.MessageId` header is not supported. Use `sendOptions.SetMessageId(...)` instead."));
        }

        class MutateOutgoingMessagesModifiesMessageIdHeader : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                context.OutgoingHeaders[Headers.MessageId] = "Some new value";
                return TaskEx.CompletedTask;
            }
        }
    }
}
