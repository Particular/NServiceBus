using System;
using NBehave.Spec.NUnit;
using NServiceBus.Host.Internal;
using NServiceBus.Saga;
using NServiceBus.Unicast.Transport.Msmq;
using NUnit.Framework;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_configuring_an_endpoint_as_a_saga_host
    {
        private Configure busConfig;

        [SetUp]
        public void SetUp()
        {
            busConfig = new ConfigurationBuilder(new SagaHostConfig(), typeof(ServerEndpoint))
                .Build();



        }

        [Test]
        public void The_nhibernate_persister_should_be_default()
        {
            busConfig.Builder.Build<SagaPersisters.NHibernate.SagaPersister>()
                .ShouldNotBeNull();
        }

        [Test]
        public void Sagas_should_be_enabled()
        {
            busConfig.Builder.Build<ServerSaga>()
                .ShouldNotBeNull();
        }

        [Test]
        public void The_user_can_specify_his_own_saga_persister()
        {
            new ConfigurationBuilder(new ServerEndpointConfigWithCustomSagaPersister(), typeof(ServerEndpoint))
                .Build()
                .Builder.Build<FakePersister>().ShouldNotBeNull();
        }
    }

    public class SagaHostConfig : IConfigureThisEndpoint, As.aSagaHost
    {
    }


    public class ServerSaga : Saga<ServerSagaData>
    {
        public override void Timeout(object state)
        {
            throw new NotImplementedException();
        }
    }

    public class ServerSagaData : ISagaEntity
    {
        public virtual Guid Id
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual string Originator
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual string OriginalMessageId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}