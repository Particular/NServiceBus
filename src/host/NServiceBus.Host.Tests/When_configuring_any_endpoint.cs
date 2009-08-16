using System.Linq;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Sagas.Impl;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_configuring_any_endpoint
    {
        [Test]
        public void XmlSerialization_can_be_requested()
        {
            var configure = Util.Init<EndpointWithXmlSerialization>();

            configure.Builder.Build<Serializers.XML.MessageSerializer>().ShouldNotBeNull();
        }

        [Test]
        public void XmlSerialization_using_a_custom_namespace_can_be_requested()
        {
            var configure = Util.Init<EndpointWithXmlSerialization>();

            configure.Builder.Build<Serializers.XML.MessageSerializer>().Namespace.ShouldEqual("testnamespace");
        }

        [Test]
        public void Ordering_of_messagehandlers_can_be_specified()
        {
            var configure = Util.Init<EndpointWithMessageHandlerOrdering>();
            var allHandlers = configure.Builder.BuildAll<IMessageHandler<IMessage>>();

            allHandlers.ElementAt(0).ShouldBeInstanceOfType(typeof (GridInterceptingMessageHandler));
            allHandlers.ElementAt(1).ShouldBeInstanceOfType(typeof(SagaMessageHandler));
        }
        [Test]
        public void A_alternate_config_source_can_be_specified()
        {
            var configure = Util.Init<EndpointWithOwnConfigSource>();

            Configure.ConfigurationSource.ShouldBeInstanceOfType(typeof (TestConfigSource));
        }

    }

    public class EndpointWithMessageHandlerOrdering : IConfigureThisEndpoint,As.aServer, ISpecify.MessageHandlerOrdering, IDontWant.Sagas
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>
                .Then<SagaMessageHandler>());
        }
    }

    public class EndpointWithXmlSerialization : IConfigureThisEndpoint,ISpecify.ToUseXmlSerialization,ISpecify.XmlSerializationNamespace, IDontWant.Sagas 
    {
        public string Namespace
        {
            get { return "testnamespace"; }
        }
    }

    public class EndpointWithOwnConfigSource : IConfigureThisEndpoint,ISpecify.MyOwnConfigurationSource, IDontWant.Sagas 
    {
       public IConfigurationSource Source
        {
            get 
            {
                return new TestConfigSource();
            }
        }
    }

    public class TestConfigSource : IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class
        {
            return null;
        }
    }
}