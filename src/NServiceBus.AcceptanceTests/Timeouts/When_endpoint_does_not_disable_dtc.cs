
namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Transports;
    using NUnit.Framework;

    class When_endpoint_does_not_disable_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_start_with_msmq()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MsmqEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }

        [Test]
        public void Endpoint_should_start_with_sqlTransport()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MsmqEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }

        [Test]
        public void Endpoint_should_not_start_with_rabbitTransport()
        {
            var context = new Context();

            var exception = Assert.Throws<AggregateException>(() => Scenario.Define(context)
                .WithEndpoint<RabbitEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run())
                .InnerException as ScenarioException;

            Assert.IsFalse(context.EndpointsStarted);
            StringAssert.Contains("You are using an outdated timeout dispatch implementation which can lead to message loss!", exception.Message);
        }

        public class MsmqEndpoint : EndpointConfigurationBuilder
        {
            public MsmqEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseTransport<Msmq>();
                });
            }
        }

        public class SqlEndpoint : EndpointConfigurationBuilder
        {
            public SqlEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseTransport<SqlServerFakeTransport>();
                });
            }
        }

        public class SqlServerFakeTransport : TransportDefinition
        {
        }

        public class SqlServerFakeTransportConfiguration : ConfigureTransport<SqlServerFakeTransport>
        {
            protected override void InternalConfigure(Configure config) { } 

            protected override string ExampleConnectionStringForErrorMessage { get; }
        }

        public class RabbitEndpoint : EndpointConfigurationBuilder
        {
            public RabbitEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseTransport<RabbitMQFakeTrasport>();
                });
            }
        }

        public class RabbitMQFakeTrasport : TransportDefinition
        {
        }

        public class RabbitMQFakeTransportConfiguration : ConfigureTransport<RabbitMQFakeTrasport>
        {
            protected override void InternalConfigure(Configure config) { }

            protected override string ExampleConnectionStringForErrorMessage { get; }
        }

        public class Context : ScenarioContext
        {
        }
    }
}
