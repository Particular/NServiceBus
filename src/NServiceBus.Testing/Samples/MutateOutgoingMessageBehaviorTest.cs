namespace NServiceBus.Testing.Samples
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Testing.Fakes;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class MutateOutgoingMessageBehaviorTest
    {
        [Test]
        public async Task ShouldInvokeNextBehavior()
        {
            var nextCalled = false;
            var testee = new MutateOutgoingMessageBehavior();
            var context = new TestableOutgoingLogicalMessageContext();

            await testee.Invoke(context, () =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.IsTrue(nextCalled);
        }

        [Test]
        public async Task ShouldAllowMutatorsToMutateMessage()
        {
            var testee = new MutateOutgoingMessageBehavior();

            var builder = new FakeBuilder();
            builder.Register<IMutateOutgoingMessages>(new DemoMutator());
            var context = new TestableOutgoingLogicalMessageContext
            {
                Builder = builder
            };

            await testee.Invoke(context, () => Task.CompletedTask);

            Assert.AreEqual("SomeValue", context.Headers["SomeHeader"]);
            Assert.AreEqual("new message", context.UpdatedMessageInstances.Single());
        }

        [Test]
        public async Task ShouldProvideIncomingMessageDataToMutators()
        {
            var expectedIncomingMessage = new object();
            var expectedIncomingHeaders = new Dictionary<string, string> { {"someKey", "someValue"} };
            var testee = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage(Guid.NewGuid().ToString(), expectedIncomingHeaders, new MemoryStream()));
            context.Extensions.Set(new LogicalMessage(new MessageMetadata(typeof(object)), expectedIncomingMessage, null));

            var builder = new FakeBuilder();
            var mutator = new StubMutator();
            builder.Register<IMutateOutgoingMessages>(mutator);
            context.Builder = builder;

            await testee.Invoke(context, () => Task.CompletedTask);

            IReadOnlyDictionary<string, string> incomingHeaders;
            object incomingMessage;
            Assert.IsTrue(mutator.MutatorContext.TryGetIncomingHeaders(out incomingHeaders));
            Assert.IsTrue(mutator.MutatorContext.TryGetIncomingMessage(out incomingMessage));
            Assert.AreEqual(expectedIncomingHeaders, incomingHeaders);
            Assert.AreEqual(expectedIncomingMessage, incomingMessage);
        }

        class DemoMutator : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                context.OutgoingHeaders.Add("SomeHeader", "SomeValue");
                context.OutgoingMessage = "new message";
                return Task.CompletedTask;
            }
        }

        class StubMutator : IMutateOutgoingMessages
        {
            public MutateOutgoingMessageContext MutatorContext { get; private set; }

            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                MutatorContext = context;
                return Task.CompletedTask;
            }
        }
    }

#region testee

    class MutateOutgoingMessageBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override async Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            IncomingMessage incomingMessage;
            context.Extensions.TryGet(out incomingMessage);
            LogicalMessage logicalMessage;
            context.Extensions.TryGet(out logicalMessage);

            var mutatorContext = new MutateOutgoingMessageContext(
                context.Message.Instance,
                context.Headers,
                logicalMessage?.Instance,
                incomingMessage?.Headers);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.OutgoingMessage);
            }

            await next().ConfigureAwait(false);
        }
    }

    public class MutateOutgoingMessageContext
    {
        object outgoingMessage;
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateOutgoingMessageContext(object outgoingMessage, IDictionary<string, string> outgoingHeaders, object incomingMessage, IReadOnlyDictionary<string, string> incomingHeaders)
        {
            OutgoingHeaders = outgoingHeaders;
            this.incomingMessage = incomingMessage;
            this.incomingHeaders = incomingHeaders;
            this.outgoingMessage = outgoingMessage;
        }

        /// <summary>
        /// The current outgoing message.
        /// </summary>
        public object OutgoingMessage
        {
            get
            {
                return outgoingMessage;
            }
            set
            {
                MessageInstanceChanged = true;
                outgoingMessage = value;
            }
        }

        internal bool MessageInstanceChanged;
        object incomingMessage;
        IReadOnlyDictionary<string, string> incomingHeaders;

        /// <summary>
        /// The current outgoing headers.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders { get; private set; }

        /// <summary>
        /// Gets the incoming message that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingMessage(out object incomingMessage)
        {
            incomingMessage = this.incomingMessage;
            return incomingMessage != null;
        }

        /// <summary>
        /// Gets the incoming headers that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingHeaders(out IReadOnlyDictionary<string, string> incomingHeaders)
        {
            incomingHeaders = this.incomingHeaders;
            return incomingHeaders != null;
        }
    }

    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized.
        /// </summary>
        Task MutateOutgoing(MutateOutgoingMessageContext context);
    }

    #endregion
}