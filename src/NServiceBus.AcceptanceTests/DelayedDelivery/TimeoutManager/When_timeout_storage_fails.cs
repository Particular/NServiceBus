namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class When_timeout_storage_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retry_and_move_to_error()
        {
            await Scenario.Define<Context>()
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
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c => Assert.AreEqual(5, c.NumTimesStorageCalled))
                .Run();
        }

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
                Context testContext;

                public FakeTimeoutStorage(Context context)
                {
                    testContext = context;
                }

                public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
                {
                    return Task.FromResult(new TimeoutsChunk(new List<TimeoutsChunk.Timeout>(), DateTime.UtcNow + TimeSpan.FromSeconds(10)));
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
            }
        }

        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName(ErrorQueueForTimeoutErrors));
            }

            class Handler : IHandleMessages<MyMessage>
            {
                public Context MyContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    MyContext.FailedTimeoutMovedToError = true;
                    return Task.FromResult(0);
                }
            }
        }

        const string ErrorQueueForTimeoutErrors = "timeout_store_errors";


        public class MyMessage : IMessage
        {
        }
    }
}
