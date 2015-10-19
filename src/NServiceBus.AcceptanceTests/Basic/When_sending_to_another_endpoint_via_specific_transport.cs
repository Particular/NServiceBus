namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_sending_to_another_endpoint_via_specific_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var queue = new BlockingCollection<FakeMessage>();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.CustomConfig(c =>
                {
                    c.GetSettings().Set("in-queue", new BlockingCollection<FakeMessage>());
                    c.GetSettings().Set("out-queue", queue);
                }).When((bus, c) => bus.SendAsync(new MyMessage(), new SendOptions())))
                .WithEndpoint<FakeTransportReceiver>(b => b.CustomConfig(c =>
                {
                    c.GetSettings().Set("in-queue", queue);
                    c.GetSettings().Set("out-queue", new BlockingCollection<FakeMessage>());
                }))
                .WithEndpoint<MsmqReceiver>()
                .Done(c => c.SentViaMsmq && c.SentViaFakeTransport)
                .Run();

            Assert.True(context.SentViaMsmq, "The message handler should be called");
            Assert.True(context.SentViaFakeTransport, "The message handler should be called");
        }

        public class Context : ScenarioContext
        {
            public bool SentViaMsmq { get; set; }
            public bool SentViaFakeTransport { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<FakeTransport>();
                    c.UseAdditionalOutgoingTransport<MsmqTransport>();

                    var msmqEndpoint = new EndpointName("SendingToAnotherEndpointViaSpecificTransport.MsmqReceiver");
                    var fakeTransportEndpoint = new EndpointName("SendingToAnotherEndpointViaSpecificTransport.FakeTransportReceiver");

                    c.Routing().UnicastRoutingTable.AddStatic(typeof(MyMessage), msmqEndpoint);
                    c.Routing().UnicastRoutingTable.AddStatic(typeof(MyMessage), fakeTransportEndpoint);

                    c.Routing().EndpointInstances.AddStatic(msmqEndpoint, new EndpointInstanceData(new EndpointInstanceName(msmqEndpoint, null, null)).UseTransport<MsmqTransport>());
                    c.Routing().EndpointInstances.AddStatic(fakeTransportEndpoint, new EndpointInstanceData(new EndpointInstanceName(fakeTransportEndpoint, null, null)));
                });
            }
        }

        public class MsmqReceiver : EndpointConfigurationBuilder
        {
            public MsmqReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message)
                {
                    Context.SentViaMsmq = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class FakeTransportReceiver : EndpointConfigurationBuilder
        {
            public FakeTransportReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<FakeTransport>();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message)
                {
                    Context.SentViaFakeTransport = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }

        class FakeMessage
        {
            public byte[] Body { get; }
            public Dictionary<string, string> Headers { get; }
            public string MessageId { get; }

            public FakeMessage(string messageId, Dictionary<string, string> headers, byte[] body)
            {
                this.MessageId = messageId;
                this.Headers = headers;
                this.Body = body;
            }
        }

        class FakeTransport : TransportDefinition
        {
            class FakeDispatcher : IDispatchMessages
            {
                readonly BlockingCollection<FakeMessage> queue;

                public FakeDispatcher(BlockingCollection<FakeMessage> queue)
                {
                    this.queue = queue;
                }

                public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ContextBag context)
                {
                    foreach (var operation in outgoingMessages)
                    {
                        queue.Add(new FakeMessage(operation.Message.MessageId, operation.Message.Headers, operation.Message.Body));
                    }
                    return Task.FromResult(0);
                }
            }

            class FakePump : IPushMessages
            {
                BlockingCollection<FakeMessage> queue;
                Func<PushContext, Task> pipe;
                Task pushTask;
                CancellationTokenSource tokenSource;

                public FakePump(BlockingCollection<FakeMessage> queue)
                {
                    this.queue = queue;
                }

                public void Init(Func<PushContext, Task> pipe, PushSettings settings)
                {
                    this.pipe = pipe;
                }

                public void Start(PushRuntimeSettings limitations)
                {
                    tokenSource = new CancellationTokenSource();
                    pushTask = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var consumer = queue.GetConsumingEnumerable(tokenSource.Token);
                            foreach (var msg in consumer)
                            {
                                using (var memStream = new MemoryStream(msg.Body))
                                {
                                    pipe(new PushContext(msg.MessageId, msg.Headers, memStream, new ContextBag())).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            //Ignore
                        }
                    });
                }

                public Task Stop()
                {
                    tokenSource.Cancel();
                    pushTask.Wait();
                    return Task.FromResult(0);
                }
            }

            class FakeQueueCreator : ICreateQueues
            {
                public void CreateQueueIfNecessary(string address, string account)
                {
                }
            }

            protected override void ConfigureForReceiving(TransportReceivingConfigurationContext context)
            {
                context.SetMessagePumpFactory(_ => new FakePump((BlockingCollection<FakeMessage>)context.Settings.Get("in-queue")));
                context.SetQueueCreatorFactory(() => new FakeQueueCreator());
            }

            protected override void ConfigureForSending(TransportSendingConfigurationContext context)
            {
                context.SetDispatcherFactory(() => new FakeDispatcher((BlockingCollection<FakeMessage>)context.GlobalSettings.Get("out-queue")));
            }

            public override IEnumerable<Type> GetSupportedDeliveryConstraints()
            {
                yield break;
            }

            public override TransactionSupport GetTransactionSupport()
            {
                return TransactionSupport.None;
            }

            public override IManageSubscriptions GetSubscriptionManager()
            {
                throw new NotImplementedException();
            }

            public override string GetDiscriminatorForThisEndpointInstance()
            {
                return null;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
            {
                return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
            }

            public override string ExampleConnectionStringForErrorMessage => null;

            public override bool RequiresConnectionString => false;
        }
    }
}
