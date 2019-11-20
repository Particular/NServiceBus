namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    using System;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_send_only_endpoint_uses_migration_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_InvalidOperationException_on_subscribe()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<SendOnlyEndpoint>(c => c
                    .When(s => s.Subscribe<SomeEvent>()))
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Send-only endpoints cannot subscribe to events", exception.Message);
        }

        [Test]
        public void Should_throw_InvalidOperationException_on_unsubscribe()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<SendOnlyEndpoint>(c => c
                    .When(s => s.Unsubscribe<SomeEvent>()))
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Send-only endpoints cannot unsubscribe to events", exception.Message);
        }

        class SendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public SendOnlyEndpoint()
            {
                EndpointSetup<EndpointWithNativePubSub>(c =>
                {
                    c.GetSettings().Set("NServiceBus.Subscriptions.EnableMigrationMode", true);
                    c.SendOnly();
                });
            }
        }

        public class SomeEvent : IEvent
        {
        }
    }
}