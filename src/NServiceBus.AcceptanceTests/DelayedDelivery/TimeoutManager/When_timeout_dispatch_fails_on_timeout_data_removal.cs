namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Persistence;
    using ScenarioDescriptors;
    using Timeout.Core;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_timeout_dispatch_fails_on_timeout_data_removal : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_move_control_message_to_errors_and_not_dispatch_original_message_to_handler()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.DoNotFailOnErrorMessages()
                    .When((bus, c) =>
                    {
                        var options = new SendOptions();
                        options.DelayDeliveryWith(TimeSpan.FromMinutes(1));
                        options.RouteToThisEndpoint();
                        options.SetMessageId(c.TestRunId.ToString());

                        return bus.Send(new MyMessage
                        {
                            Id = c.TestRunId
                        }, options);
                    }))
                .Done(c => c.FailedTimeoutMovedToError)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferralAndWithAtomicSendAndReceive>())
                .Should(c => Assert.IsFalse(c.DelayedMessageDeliveredToHandler, "Message was unexpectedly delivered to the handler"))
                .Run();
        }

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
                    config.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            class Handler : IHandleMessages<MyMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.TestRunId)
                    {
                        TestContext.DelayedMessageDeliveredToHandler = true;
                    }

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
                    throw new Exception("Simulated exception on removing timeout data.");
                }

                public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
                {
                    if (timeoutId == TestContext.TestRunId.ToString() && timeoutData != null)
                    {
                        return Task.FromResult(timeoutData);
                    }

                    return Task.FromResult((TimeoutData) null);
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
            public Guid Id { get; set; }
        }
    }
}