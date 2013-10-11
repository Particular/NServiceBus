namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Mono.CSharp;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxTests
    {
        FuncBuilder funcBuilder;
        FakeOutboxStorage fakeOutbox;
        BehaviorChain pipeline;

        [SetUp]
        public void SetUp()
        {
            Configure.With(new Assembly[0])
                     .DefineEndpointName("Test")
                     .DefaultBuilder();

            funcBuilder = new FuncBuilder();

            fakeOutbox = new FakeOutboxStorage();
            funcBuilder.Register<IOutboxStorage>(() => fakeOutbox);
            funcBuilder.Register<OutboxBehaviour>(() => new OutboxBehaviour());//{ Builder = funcBuilder }

            pipeline = new BehaviorChain(() => funcBuilder);

            pipeline.Add<OutboxBehaviour>();

        }


        [Test]
        public void Should_mark_outbox_message_as_stored_when_successfully_processing_a_message()
        {
            var incomingTransportMessage = new TransportMessage();

            pipeline.Invoke(incomingTransportMessage);

            Assert.True(fakeOutbox.StoredMessage.Dispatched);
        }

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage { Id = incomingTransportMessage.Id };

            pipeline.Add<BlowUpIfInvoked>();
            pipeline.Invoke(incomingTransportMessage);

            Assert.True(fakeOutbox.ExistingMessage.Dispatched);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        class OutboxBehaviour : IBehavior
        {
            public IBehavior Next { get; set; }

            public IOutboxStorage OutboxStorage { get; set; }

            public void Invoke(IBehaviorContext context)
            {
                var messageId = context.TransportMessage.Id;

                var outboxMessage = OutboxStorage.Get(messageId);

                if (outboxMessage == null)
                {
                    outboxMessage = new OutboxMessage { Id = messageId };

                    context.Set(outboxMessage);

                    //this runs the rest of the pipeline
                    Next.Invoke(context);

                    OutboxStorage.Store(outboxMessage);
                }

                DispachOperationToTransport(outboxMessage.TransportOperations);

                OutboxStorage.SetAsDispatched(outboxMessage);
            }

            void DispachOperationToTransport(IEnumerable<TransportOperation> transportOperations)
            {
                foreach (var transportOperation in transportOperations)
                {
                    //dispatch to transport
                }
            }
        }
    }

    public class BlowUpIfInvoked : IBehavior
    {
        public IBehavior Next { get; set; }
        public void Invoke(IBehaviorContext context)
        {
            throw new Exception("Pipeline should have been aborted");
        }
    }

    internal class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage Get(string messageId)
        {
            if (ExistingMessage != null && ExistingMessage.Id == messageId)
                return ExistingMessage;

            return null;
        }

        public void Store(OutboxMessage outboxMessage)
        {
            StoredMessage = outboxMessage;
        }

        public void SetAsDispatched(OutboxMessage outboxMessage)
        {
            if (StoredMessage != null)
            {
                if (StoredMessage.Id == outboxMessage.Id)
                    StoredMessage.Dispatched = true;
            }

            if (ExistingMessage != null)
            {
                if (ExistingMessage.Id == outboxMessage.Id)
                    ExistingMessage.Dispatched = true;
            }
        }

        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }
    }

    internal interface IOutboxStorage
    {
        OutboxMessage Get(string messageId);
        void Store(OutboxMessage outboxMessage);
        void SetAsDispatched(OutboxMessage outboxMessage);
    }

    internal class OutboxMessage
    {
        public string Id { get; set; }

        public List<TransportOperation> TransportOperations
        {
            get
            {
                if (transportOperations == null)
                    transportOperations = new List<TransportOperation>();

                return transportOperations;
            }
            protected set
            {
                transportOperations = value;
            }
        }

        List<TransportOperation> transportOperations;
        public bool Dispatched { get; set; }
    }

    internal class TransportOperation
    {
    }
}