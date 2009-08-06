using System;
using System.Collections.Generic;
using NBehave.Spec.NUnit;
using NServiceBus.Host.Internal;
using NServiceBus.Saga;
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
            busConfig = new ConfigurationBuilder(new SagaHostConfig())
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
            new ConfigurationBuilder(new ServerEndpointConfigWithCustomSagaPersister())
                .Build()
                .Builder.Build<FakePersister>().ShouldNotBeNull();
        }
    }

    public class SagaHostConfig : IConfigureThisEndpoint,
        ISpecify.TypesToScan
    {
        public IEnumerable<Type> TypesToScan
        {
            get { return new[] { typeof(SagaPersisters.NHibernate.SagaPersister), typeof(ServerSaga), typeof(ServerSagaData) }; }
        }
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