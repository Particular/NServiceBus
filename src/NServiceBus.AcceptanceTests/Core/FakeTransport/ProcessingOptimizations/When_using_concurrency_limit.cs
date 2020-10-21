using System;
using System.Linq;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport.ProcessingOptimizations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;
    using Transport;

    public class When_using_concurrency_limit : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_pass_it_to_the_transport()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<ThrottledEndpoint>(b => b.CustomConfig(c => c.LimitMessageProcessingConcurrencyTo(10)))
                .Done(c => c.EndpointsStarted)
                .Run();

            //Assert in FakeReceiver.Start
        }

        class ThrottledEndpoint : EndpointConfigurationBuilder
        {
            public ThrottledEndpoint()
            {
                var template = new DefaultServer
                {
                    PersistenceConfiguration = new ConfigureEndpointInMemoryPersistence()
                };

                EndpointSetup(template, (endpointConfiguration, _) => endpointConfiguration.UseTransport(new FakeTransport()));
            }
        }

        class FakeReceiver : IPushMessages
        {
            PushSettings pushSettings;

            public FakeReceiver(PushSettings pushSettings, string id)
            {
                Id = id;
                this.pushSettings = pushSettings;
            }

            public void Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
            {
                // The LimitMessageProcessingConcurrencyTo setting only applies to the input queue
                if (pushSettings.InputQueue == Conventions.EndpointNamingConvention(typeof(ThrottledEndpoint)))
                {
                    Assert.AreEqual(10, limitations.MaxConcurrency);
                }
            }

            public Task Stop()
            {
                return Task.FromResult(0);
            }

            public IManageSubscriptions Subscriptions { get; }
            public string Id { get; }
        }

        class FakeDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
            {
                return Task.FromResult(0);
            }
        }

        class FakeTransport : TransportDefinition
        {
            public override Task<TransportInfrastructure> Initialize(Settings settings, ReceiveSettings[] receiveSettings)
            {
                return Task.FromResult<TransportInfrastructure>(new FakeTransportInfrastructure(receiveSettings));
            }

            public override string ToTransportAddress(EndpointAddress address)
            {
                return address.ToString();
            }

            public override TransportTransactionMode MaxSupportedTransactionMode { get; } = TransportTransactionMode.None;

            public override bool SupportsTTBR { get; } = false;

        }

        class FakeTransportInfrastructure : TransportInfrastructure
        {
            public FakeTransportInfrastructure(ReceiveSettings[] receiveSettingses)
            {
                Receivers = receiveSettingses.Select(s => new FakeReceiver(s.settings, s.Id)).ToArray();
            }

            public override IDispatchMessages Dispatcher => new FakeDispatcher();
            public override IPushMessages[] Receivers { get; protected set; }
            public override void Dispose()
            {
            }
        }
    }
}
