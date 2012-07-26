using NServiceBus.DataBus.Config;
using NServiceBus.DataBus.Tests;
using NUnit.Framework;
using log4net;

namespace NServiceBus.DataBus.Tests
{
    using System;
    using NUnit.Framework;

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

        [Test]
        public void Should_throw_if_propertytype_is_not_serializable()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Assert.Ignore("This only work in debug mode.");
            }

            Configure.With(new[] { typeof(MessageWithNonSerializableDataBusProperty) })
                .DefineEndpointName("xyz")
                .DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"))
                .DefaultBuilder()
                .Configurer.RegisterSingleton<IDataBus>(new InMemoryDataBus());

            var bootStrapper = new Bootstrapper();

            Assert.Throws<InvalidOperationException>(bootStrapper.Init);
        }
    }
}
