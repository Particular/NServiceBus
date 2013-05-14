namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using NUnit.Framework;
 
    [TestFixture]
    public class When_configuring_the_saga_persister_to_use_sqlite
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            Configure.Features.Enable<Features.Sagas>();
         
            config = Configure.With(new[] { typeof(MySaga).Assembly})
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSagaPersister();
        }

        [Test]
        public void Persister_should_be_registered_as_single_call()
        {
            var persister = config.Builder.Build<SagaPersister>();

            Assert.AreNotEqual(persister, config.Builder.Build<SagaPersister>());
        }
    }
}
