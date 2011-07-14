using System;
using NServiceBus.SagaPersisters.Raven.Config;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_a_connection_string_that_does_not_exist
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void It_should_throw_an_exception()
        {
            Configure.With(new[] { GetType().Assembly })
                .DefaultBuilder()
                .Sagas().RavenSagaPersister("ConnectionStringDoesNotExist");
        }
    }
}