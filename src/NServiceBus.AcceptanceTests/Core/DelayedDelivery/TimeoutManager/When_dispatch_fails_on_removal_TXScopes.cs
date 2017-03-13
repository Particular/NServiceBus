namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Timeout.Core;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_dispatch_fails_on_removal_TXScopes : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_control_message_to_errors_and_not_dispatch_original_message_to_handler()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b
                    .DoNotFailOnErrorMessages()
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
                .Run();

            Assert.IsFalse(context.DelayedMessageDeliveredToHandler);
            Assert.IsTrue(context.FailedTimeoutMovedToError);
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
                EndpointSetup<DefaultServer>((config, runDescriptor) =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.UsePersistence<FakeTimeoutPersistence>();
                    config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(Endpoint)));
                    config.RegisterComponents(c => c.ConfigureComponent<FakeTimeoutStorage>(DependencyLifecycle.SingleInstance));
                    config.Pipeline.Register<BehaviorThatLogsControlMessageDelivery.Registration>();
                    config.LimitMessageProcessingConcurrencyTo(1);
                    config.ConfigureTransport().Transactions(TransportTransactionMode.TransactionScope);
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

            class BehaviorThatLogsControlMessageDelivery : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
            {
                public Context TestContext { get; set; }

                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    if (context.Message.Headers.ContainsKey(Headers.ControlMessageHeader) &&
                        context.Message.Headers["Timeout.Id"] == TestContext.TestRunId.ToString())
                    {
                        TestContext.FailedTimeoutMovedToError = true;
                        return Task.FromResult(0);
                    }

                    return next(context);
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