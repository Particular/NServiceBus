namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_using_outgoing_tm_mutator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_update_message()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocalAsync(new MessageToBeMutated())))
                    .Done(c => c.MessageProcessed)
                    .Run();

            Assert.True(context.CanAddHeaders);
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool CanAddHeaders { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyTransportMessageMutator : IMutateOutgoingTransportMessages, INeedInitialization
            {
                public void MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.SetHeader("HeaderSetByMutator", "some value");
                    context.SetHeader(Headers.EnclosedMessageTypes,typeof(MessageThatMutatorChangesTo).FullName);

                    context.RegisterStreamMutation(stream => new HardcodedBufferDecorator(Encoding.UTF8.GetBytes("<MessageThatMutatorChangesTo/>"),stream));

                    return;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageThatMutatorChangesTo>
            {
                Context testContext;
                IBus bus;

                public MessageToBeMutatedHandler(Context testContext, IBus bus)
                {
                    this.testContext = testContext;
                    this.bus = bus;
                }

                public Task Handle(MessageThatMutatorChangesTo message)
                {
                    testContext.CanAddHeaders = bus.CurrentMessageContext.Headers.ContainsKey("HeaderSetByMutator");
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }
            }

        }

        public class MessageToBeMutated : ICommand
        {
        }

        public class MessageThatMutatorChangesTo : ICommand
        {
        }
    }

    class HardcodedBufferDecorator:Stream
    {
        readonly byte[] hardcodedBuffer;
        readonly Stream decoratedStream;

        public HardcodedBufferDecorator(byte[] hardcodedBuffer, Stream decoratedStream)
        {
            this.hardcodedBuffer = hardcodedBuffer;
            this.decoratedStream = decoratedStream;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (valueWritten)
            {
                return;
            }

            decoratedStream.Write(hardcodedBuffer,0, hardcodedBuffer.Length);
            valueWritten = true;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => hardcodedBuffer.Length;
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        bool valueWritten;
    }
}