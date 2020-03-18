namespace NServiceBus.AcceptanceTests.Satellites
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using ObjectBuilder;
    using Transport;

    public class When_ripping_out_extreme : NServiceBusAcceptanceTest
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
                    c.Pipeline.Replace("TransportReceiveToPhysicalMessageProcessingConnector", new BastardizingTheCoreBehavior(OnMessage, OnError));
                    
                    c.DisableFeature<AutoSubscribe>();
                    c.DisableFeature<Sagas>();
                });
            }

            static Task<ErrorHandleResult> OnError(ITransportReceiveContext arg)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }

            static Task OnMessage(ITransportReceiveContext context)
            {
                var outgoingMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(Receiver))));
                context.AddToBatch(transportOperation);
                
                var headers = new Dictionary<string, string>
                {
                    { Headers.EnclosedMessageTypes, typeof(MyOtherMessage).AssemblyQualifiedName }
                };
                
                // reusing for this the same body
                var anotherOutgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, context.Message.Body);
                var anotherTransportOperation = new TransportOperation(anotherOutgoingMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(Receiver))));
                context.AddToBatch(anotherTransportOperation);

                // high level sends no longer possible
                // await context.Send(new MyOtherMessage()).ConfigureAwait(false);
                return Task.FromResult(0);
            }

            class BastardizingTheCoreBehavior : StageConnector<ITransportReceiveContext, IBatchDispatchContext>
            {
                Func<ITransportReceiveContext, Task> onMessage;
                Func<ITransportReceiveContext, Task<ErrorHandleResult>> onError;

                public BastardizingTheCoreBehavior(Func<ITransportReceiveContext, Task> onMessage, Func<ITransportReceiveContext, Task<ErrorHandleResult>> onError)
                {
                    this.onError = onError;
                    this.onMessage = onMessage;
                }
                
                public override async Task Invoke(ITransportReceiveContext context, Func<IBatchDispatchContext, Task> stage)
                {
                    try
                    {
                        var pendingTransportOperations = new PendingTransportOperations();
                        
                        context.Extensions.Set(pendingTransportOperations);

                        await onMessage(context).ConfigureAwait(false);
                        
                        context.Extensions.Remove<PendingTransportOperations>();
                        
                        if (pendingTransportOperations.HasOperations)
                        {
                            var batchDispatchContext = new BatchDispatchContext(pendingTransportOperations.Operations, context);

                            await stage(batchDispatchContext).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        var result = await onError(context).ConfigureAwait(false);
                        if (result == ErrorHandleResult.RetryRequired)
                        {
                            throw new Exception("Retry required", e);
                        }
                    }
                }
                
                abstract class BehaviorContext : ContextBag, IBehaviorContext
                {
                    protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
                    {
                    }

                    public IBuilder Builder => Get<IBuilder>();

                    public ContextBag Extensions => this;
                }
                
                class BatchDispatchContext : BehaviorContext, IBatchDispatchContext
                {
                    public BatchDispatchContext(IReadOnlyCollection<TransportOperation> operations, IBehaviorContext parentContext)
                        : base(parentContext)
                    {
                        Operations = operations;
                    }

                    public IReadOnlyCollection<TransportOperation> Operations { get; }
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
    
    public static class ITransportReceiveContextExtensions
    {
        public static void AddToBatch(this ITransportReceiveContext context, TransportOperation transportOperation)
        {
            var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();
            pendingTransportOperations.Add(transportOperation);
        }
        
        public static void AddToBatch(this ITransportReceiveContext context, TransportOperation[] transportOperation)
        {
            var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();
            pendingTransportOperations.AddRange(transportOperation);
        }
    }
}