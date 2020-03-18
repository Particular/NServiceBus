namespace NServiceBus.AcceptanceTests.Satellites
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    public class When_ripping_out : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage())))
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageReceived && c.OtherMessageReceived)
                .Run().ConfigureAwait(false);

            Assert.True(context.MessageReceived);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public bool OtherMessageReceived { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var transport = c.ConfigureTransport();
                    transport.Routing().RouteToEndpoint(typeof(MyOtherMessage), Conventions.EndpointNamingConvention(typeof(Receiver)));
                    
                    c.Pipeline.Replace("TransportReceiveToPhysicalMessageProcessingConnector", new BastardizingTheCoreForkConnector(OnMessage, OnError));
                    
                    c.DisableFeature<AutoSubscribe>();
                    c.DisableFeature<Sagas>();
                });
            }

            static Task<ErrorHandleResult> OnError(IIncomingPhysicalMessageContext arg)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }

            static async Task OnMessage(ITransportReceiveContext rawContext, IIncomingPhysicalMessageContext context)
            {
                var outgoingMessage = new OutgoingMessage(rawContext.Message.MessageId, rawContext.Message.Headers, rawContext.Message.Body);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(Receiver))));
                context.AddToBatch(transportOperation);

                // just to show what crazy stuff we can do now
                await context.Send(new MyOtherMessage()).ConfigureAwait(false);
            }

            class BastardizingTheCoreForkConnector : StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
            {
                Func<ITransportReceiveContext, IIncomingPhysicalMessageContext, Task> onMessage;
                Func<IIncomingPhysicalMessageContext, Task<ErrorHandleResult>> onError;

                public BastardizingTheCoreForkConnector(Func<ITransportReceiveContext, IIncomingPhysicalMessageContext, Task> onMessage, Func<IIncomingPhysicalMessageContext, Task<ErrorHandleResult>> onError)
                {
                    this.onError = onError;
                    this.onMessage = onMessage;
                }
                
                public override async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> stage, Func<IBatchDispatchContext, Task> fork)
                {
                    var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);
                    
                    try
                    {
                        var pendingTransportOperations = new PendingTransportOperations();
                        
                        physicalMessageContext.Extensions.Set(pendingTransportOperations);

                        await onMessage(context, physicalMessageContext).ConfigureAwait(false);
                        
                        physicalMessageContext.Extensions.Remove<PendingTransportOperations>();
                        
                        if (pendingTransportOperations.HasOperations)
                        {
                            var batchDispatchContext = this.CreateBatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

                            await fork(batchDispatchContext).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        var result = await onError(physicalMessageContext).ConfigureAwait(false);
                        if (result == ErrorHandleResult.RetryRequired)
                        {
                            throw new Exception("Retry required", e);
                        }
                    }
                }
            }
            
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }   
            
            
            public class MyHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public MyHandler(Context context)
                {
                    testContext = context;
                }
                
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
            
            public class MyOtherMessageHandler : IHandleMessages<MyOtherMessage>
            {
                Context testContext;

                public MyOtherMessageHandler(Context context)
                {
                    testContext = context;
                }
                
                public Task Handle(MyOtherMessage message, IMessageHandlerContext context)
                {
                    testContext.OtherMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
        
        public class MyOtherMessage : IMessage
        {
        }
    }
    
    public static class IIncomingPhysicalMessageContextExtensions
    {
        public static void AddToBatch(this IIncomingPhysicalMessageContext context, TransportOperation transportOperation)
        {
            var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();
            pendingTransportOperations.Add(transportOperation);
        }
        
        public static void AddToBatch(this IIncomingPhysicalMessageContext context, TransportOperation[] transportOperation)
        {
            var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();
            pendingTransportOperations.AddRange(transportOperation);
        }
    }
}