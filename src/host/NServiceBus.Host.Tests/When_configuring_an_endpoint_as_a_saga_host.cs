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
        [Test]
        public void The_nhibernate_persister_should_be_default()
        {
            //var configure = Util.Init<SagaHostConfig>();

            //configure.Builder.Build<SagaPersisters.NHibernate.SagaPersister>()
            //    .ShouldNotBeNull();
        }

        [Test]
        public void Sagas_should_be_enabled()
        {
            //var configure = Util.Init<SagaHostConfig>();

            //configure.Builder.Build<ServerSaga>()
            //    .ShouldNotBeNull();
        }

        [Test]
        public void The_user_can_specify_his_own_saga_persister()
        {
            //var configure = Util.Init<ServerEndpointConfigWithCustomSagaPersister>();

            //configure.Builder.Build<FakePersister>().ShouldNotBeNull();
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
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }
    }
}