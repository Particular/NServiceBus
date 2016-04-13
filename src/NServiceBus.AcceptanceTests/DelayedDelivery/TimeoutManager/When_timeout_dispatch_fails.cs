namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Timeout.Core;

    public class When_timeout_dispatch_fails : NServiceBusAcceptanceTest
    {
        static readonly TimeSpan VeryLongTimeSpan = TimeSpan.FromMinutes(10);

        [Test]
        public Task Should_retry_and_move_to_error()
        {
            return Scenario.Define<Context>()
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
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c =>
                {
                    Assert.IsFalse(c.DelayedMessageDeliveredToHandler, "Message was unexpectedly delivered to the handler");
                    Assert.IsTrue(c.FailedTimeoutMovedToError, "Message should have been moved to the error queue");
                })
                .Run();
        }

        const string ErrorQueueForTimeoutErrors = "timeout_dispatch_errors";

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
                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    TestContext.DelayedMessageDeliveredToHandler = true;
                    return Task.FromResult(0);
                }
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
                public Context TestContext { get; set; }
                TimeoutData timeoutData;

                public Task Add(TimeoutData timeout, ContextBag context)
                {
                    if (TestContext.TestRunId.ToString() == timeout.Headers[Headers.MessageId])
                    {
                        timeout.Id = TestContext.TestRunId.ToString();
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
                    if (timeoutId != TestContext.TestRunId.ToString())
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
            }

            class BehaviorThatLogsControlMessageDelivery : Behavior<ITransportReceiveContext>
            {
                public Context TestContext { get; set; }

                public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    if (context.Message.Headers.ContainsKey(Headers.ControlMessageHeader) &&
                        context.Message.Headers["Timeout.Id"] == TestContext.TestRunId.ToString())
                    {
                        TestContext.FailedTimeoutMovedToError = true;
                        return;
                    }

                    await next().ConfigureAwait(false);
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("BehaviorThatLogsControlMessageDelivery", typeof(BehaviorThatLogsControlMessageDelivery), "BehaviorThatLogsControlMessageDelivery")
                    {
                    }
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}