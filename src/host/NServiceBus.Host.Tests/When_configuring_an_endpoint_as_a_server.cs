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
            busConfig = new ConfigurationBuilder()
                .BuildConfigurationFrom(new ServerEndpointConfig(), typeof(ServerEndpoint));

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
        public void Sagas_should_be_enabled()
        {
            busConfig.Builder.Build<ServerSaga>().ShouldNotBeNull();
        }

        [Test]
        public void Saga_perister_should_default_to_in_memory()
        {
            busConfig.Builder.Build<InMemorySagaPersister>().ShouldNotBeNull();
        }

        [Test]
        public void The_bus_should_impersonate_the_sender()
        {
            var unicastbus = busConfig.Builder.Build<UnicastBus>();
            
            unicastbus.ShouldNotBeNull();
            unicastbus.ImpersonateSender.ShouldBeTrue();
        }


    }

    public class ServerEndpointConfig : IConfigureThisEndpoint,As.aServer 
    {
    }

    public class ServerEndpoint:IMessageEndpoint
    {
        public void OnStart()
        {
            throw new NotImplementedException();
        }

        public void OnStop()
        {
            throw new NotImplementedException();
        }
    }

    public class ServerSaga:ISaga<ServerSagaData>
    {
        public void Timeout(object state)
        {
            throw new NotImplementedException();
        }

        public bool Completed
        {
            get { throw new NotImplementedException(); }
        }

        public void Configure()
        {
            
        }

        public ISagaEntity Entity
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IBus Bus{ get; set;}

        public ServerSagaData Data
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class ServerSagaData : ISagaEntity
    {
        public Guid Id
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string Originator
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string OriginalMessageId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}