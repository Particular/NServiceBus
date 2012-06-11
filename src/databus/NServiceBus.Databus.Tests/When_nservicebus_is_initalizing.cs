using NServiceBus.DataBus;
using NServiceBus.DataBus.Config;
using NServiceBus.DataBus.Tests;
using NUnit.Framework;

namespace NServiceBus.DataBus.Tests
{
	using ObjectBuilder;

	[TestFixture]
    public class When_nservicebus_is_initalizing
    {
        [Test]
        public void Databus_mutators_should_be_registered_if_a_databus_property_is_found()
        {
            Configure.With(new[] {typeof (MessageWithDataBusProperty)})
                .DefineEndpointName("xyz") 
                .DefaultBuilder();


            var bootStrapper = new Bootstrapper();

        	Configure.Instance.Configurer.ConfigureComponent<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            bootStrapper.Init();

            Assert.True(Configure.Instance.Configurer.HasComponent<DataBusMessageMutator>());
        }

        [Test]
        public void Databus_mutators_should_not_be_registered_if_no_databus_property_is_found()
        {
            Configure.With(new[] { typeof(MessageWithoutDataBusProperty) })
                .DefineEndpointName("xyz") 
                .DefaultBuilder();

            var bootStrapper = new Bootstrapper();

            bootStrapper.Init();

            Assert.False(Configure.Instance.Configurer.HasComponent<DataBusMessageMutator>());
        }

        public class MessageWithoutDataBusProperty
        {
            public string SomeProperty { get; set; }
        }

    }
}