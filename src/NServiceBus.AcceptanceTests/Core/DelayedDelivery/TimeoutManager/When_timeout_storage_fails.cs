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
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    
    public class When_timeout_storage_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retry_and_move_to_error()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.DoNotFailOnErrorMessages()
                    .When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DelayDeliveryWith(TimeSpan.FromDays(30));

                        options.RouteToThisEndpoint();

                        return bus.Send(new MyMessage(), options);
                    }))
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.FailedTimeoutMovedToError)
                .Run();

            Assert.AreEqual(5, context.NumTimesStorageCalled);
        }

        const string ErrorQueueForTimeoutErrors = "timeout_store_errors";

        public class Context : ScenarioContext
        {
            public bool FailedTimeoutMovedToError { get; set; }
            public int NumTimesStorageCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.UsePersistence<FakeTimeoutPersistence>();
                    config.SendFailedMessagesTo(ErrorQueueForTimeoutErrors);
                    config.RegisterComponents(c => c.ConfigureComponent<FakeTimeoutStorage>(DependencyLifecycle.SingleInstance));
                });
            }

            public class FakeTimeoutPersistence : PersistenceDefinition
            {
                public FakeTimeoutPersistence()
                {
                    Supports<StorageType.Timeouts>(s => { });
                }
            }

            public class FakeTimeoutStorage : IQueryTimeouts, IPersistTimeouts
            {
                public FakeTimeoutStorage(Context context)
                {
                    testContext = context;
                }

                public Task Add(TimeoutData timeout, ContextBag context)
                {
                    testContext.NumTimesStorageCalled++;

                    throw new Exception("Simulated exception on storing timeout.");
                }

                public Task<bool> TryRemove(string timeoutId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
                {
                    return Task.FromResult(new TimeoutsChunk(new TimeoutsChunk.Timeout[0], DateTime.UtcNow + TimeSpan.FromSeconds(10)));
                }

                Context testContext;
            }
        }

        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName(ErrorQueueForTimeoutErrors);
            }

            class Handler : IHandleMessages<MyMessage>
            {
                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.FailedTimeoutMovedToError = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}