namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using NServiceBus.DataBus;
    using NServiceBus.DataBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_nservicebus_is_initializing
    {
        [Test]
        public void Databus_mutators_should_be_registered_if_a_databus_property_is_found()
        {
            Configure.With(new[] {typeof (MessageWithDataBusProperty)})
                .DefineEndpointName("xyz") 
                .DefaultBuilder();

            IWantToRunBeforeConfigurationIsFinalized bootstrapper = new Bootstrapper();

        	Configure.Instance.Configurer.ConfigureComponent<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            bootstrapper.Run();

            Assert.True(Configure.Instance.Configurer.HasComponent<DataBusMessageMutator>());
        }

        [Test]
        public void Databus_mutators_should_not_be_registered_if_no_databus_property_is_found()
        {
            Configure.With(new[] { typeof(MessageWithoutDataBusProperty) })
                .DefineEndpointName("xyz") 
                .DefaultBuilder();

            IWantToRunBeforeConfigurationIsFinalized bootstrapper = new Bootstrapper();

            bootstrapper.Run();

            Assert.False(Configure.Instance.Configurer.HasComponent<DataBusMessageMutator>());
        }

        [Test]
        public void Should_throw_if_propertyType_is_not_serializable()
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

            IWantToRunBeforeConfigurationIsFinalized bootstrapper = new Bootstrapper();

            Assert.Throws<InvalidOperationException>(bootstrapper.Run);
        }

        [Test]
        public void Should_not_throw_propertyType_is_not_serializable_if_a_IDataBusSerializer_is_already_registered()
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

            IWantToRunBeforeConfigurationIsFinalized bootstrapper = new Bootstrapper();

            Configure.Instance.Configurer.ConfigureComponent<IDataBusSerializer>(() => new MyDataBusSerializer(),DependencyLifecycle.SingleInstance);

            Assert.DoesNotThrow(bootstrapper.Run);
        }

        class MyDataBusSerializer : IDataBusSerializer
        {
            public void Serialize(object databusProperty, Stream stream)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(Stream stream)
            {
                throw new NotImplementedException();
            }
        }
    }

    
}
