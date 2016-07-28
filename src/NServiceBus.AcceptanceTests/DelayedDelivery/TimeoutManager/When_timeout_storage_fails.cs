﻿namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus;
    using NUnit.Framework;
    using Persistence;
    using ScenarioDescriptors;
    using Timeout.Core;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_timeout_storage_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_retry_and_move_to_error()
        {
            return Scenario.Define<Context>()
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
                    config.Recoverability().Failed(failed => failed.SendTo(Conventions.NameOf<ErrorSpy>()));
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
                EndpointSetup<DefaultServer>();
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

        public class MyMessage : IMessage
        {
        }
    }
}