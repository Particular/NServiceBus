using System;
using NServiceBus.Host.Internal;
using NServiceBus.Saga;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Msmq;
using NUnit.Framework;
using NBehave.Spec.NUnit;
using Rhino.Mocks;

namespace NServiceBus.Host.Tests
{
     
    [TestFixture]
    public class When_configuring_an_endpoint_as_a_server
    {
        private Configure busConfig;
        private MsmqTransport transport;
        
        [SetUp]
        public void SetUp()
        {
            busConfig = new ConfigurationBuilder(new ServerEndpointConfig(), typeof(ServerEndpoint))
                .Build();

            transport = busConfig.Builder.Build<ITransport>() as MsmqTransport;

        }

        [Test]
        public void Transport_should_be_msmq()
        {
            transport.ShouldNotBeNull();
        }

        [Test]
        public void Transport_should_be_transactional()
        {
            transport.IsTransactional.ShouldBeTrue();
        }

        [Test]
        public void Transport_should_not_be_purged_on_startup()
        {
            transport.PurgeOnStartup.ShouldBeFalse();
        }

     
        [Test]
        public void The_bus_should_impersonate_the_sender()
        {
            var unicastbus = busConfig.Builder.Build<UnicastBus>();
            
            unicastbus.ShouldNotBeNull();
            unicastbus.ImpersonateSender.ShouldBeTrue();
        }



    }
}