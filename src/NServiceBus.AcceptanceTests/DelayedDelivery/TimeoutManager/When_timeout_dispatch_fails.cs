namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Timeout.Core;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_timeout_dispatch_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retry_and_move_to_error()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.DoNotFailOnErrorMessages()
                        .When((bus, c) =>
                        {
                            var options = new SendOptions();
                            options.SetHeader("Timeout.Id", Guid.NewGuid().ToString());
                            options.SetDestination(Conventions.EndpointNamingConvention(typeof(Endpoint)) + ".TimeoutsDispatcher");
                            return bus.Send(new MyMessage(), options);
                        }))
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.FailedTimeoutMovedToError)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c => Assert.AreEqual(5, c.NumTimesStorageCalled))
                .Run();
        }

        const string ErrorQueueForTimeoutErrors = "timeout_dispatch_errors";

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
                    return Task.FromResult(0);
                }

                public Task<bool> TryRemove(string timeoutId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
                {
                    TestContext.NumTimesStorageCalled++;
                    throw new Exception("Ooops. Timeout storage went bust");
                }

                public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
                {
                    throw new NotImplementedException();
                }

                public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
                {
                    return Task.FromResult(new TimeoutsChunk(Enumerable.Empty<TimeoutsChunk.Timeout>(), DateTime.MaxValue));
                }
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
                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    TestContext.FailedTimeoutMovedToError = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}