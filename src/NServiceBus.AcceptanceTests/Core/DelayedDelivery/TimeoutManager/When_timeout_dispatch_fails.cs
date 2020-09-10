namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_timeout_dispatch_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retry_and_move_to_error()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.DoNotFailOnErrorMessages()
                        .When((bus, c) =>
                        {
                            var options = new SendOptions();
                            options.DelayDeliveryWith(VeryLongTimeSpan);
                            options.RouteToThisEndpoint();
                            options.SetMessageId(c.TestRunId.ToString());

                            return bus.Send(new MyMessage(), options);
                        }))
                .Done(c => c.FailedTimeoutMovedToError)
                .Run();

            Assert.IsFalse(context.DelayedMessageDeliveredToHandler, "Message was unexpectedly delivered to the handler");
            Assert.IsTrue(context.FailedTimeoutMovedToError, "Message should have been moved to the error queue");
        }

        static readonly TimeSpan VeryLongTimeSpan = TimeSpan.FromMinutes(10);
        public class Context : ScenarioContext
        {
            public bool FailedTimeoutMovedToError { get; set; }
            public bool DelayedMessageDeliveredToHandler { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.UsePersistence<FakeTimeoutPersistence>();
                    config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(Endpoint)));
                    config.RegisterComponents(c => c.ConfigureComponent<FakeTimeoutStorage>(DependencyLifecycle.SingleInstance));
                    config.Pipeline.Register<BehaviorThatLogsControlMessageDelivery.Registration>();
                });
            }

            class Handler : IHandleMessages<MyMessage>
            {
                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.DelayedMessageDeliveredToHandler = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class FakeTimeoutPersistence : PersistenceDefinition
            {
                public FakeTimeoutPersistence()
                {
                    Supports<StorageType.Timeouts>(s => { });
                }
            }

            class FakeTimeoutStorage : IQueryTimeouts, IPersistTimeouts
            {
                public FakeTimeoutStorage(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Add(TimeoutData timeout, ContextBag context)
                {
                    if (testContext.TestRunId.ToString() == timeout.Headers[Headers.MessageId])
                    {
                        timeout.Id = testContext.TestRunId.ToString();
                        timeout.Time = DateTime.UtcNow;

                        timeoutData = timeout;
                    }

                    return Task.FromResult(0);
                }

                public Task<bool> TryRemove(string timeoutId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
                {
                    if (timeoutId != testContext.TestRunId.ToString())
                    {
                        throw new ArgumentException("The timeoutId is different from one registered in the test context", "timeoutId");
                    }

                    throw new Exception("Ooops. Timeout storage went bust");
                }

                public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
                {
                    var timeouts = timeoutData != null
                        ? new[]
                        {
                            new TimeoutsChunk.Timeout(timeoutData.Id, timeoutData.Time)
                        }
                        : new TimeoutsChunk.Timeout[0];

                    return Task.FromResult(new TimeoutsChunk(timeouts, DateTime.UtcNow + TimeSpan.FromSeconds(10)));
                }

                TimeoutData timeoutData;
                Context testContext;
            }

            class BehaviorThatLogsControlMessageDelivery : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
            {
                public BehaviorThatLogsControlMessageDelivery(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken cancellationToken)
                {
                    if (context.Message.Headers.ContainsKey(Headers.ControlMessageHeader) &&
                        context.Message.Headers["Timeout.Id"] == testContext.TestRunId.ToString())
                    {
                        testContext.FailedTimeoutMovedToError = true;
                        return Task.FromResult(0);
                    }

                    return next(context, cancellationToken);
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("BehaviorThatLogsControlMessageDelivery", typeof(BehaviorThatLogsControlMessageDelivery), "BehaviorThatLogsControlMessageDelivery")
                    {
                    }
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}