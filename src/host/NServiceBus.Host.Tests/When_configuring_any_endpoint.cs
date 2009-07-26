using System.Linq;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host.Internal;
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
            var busConfig = new ConfigurationBuilder()
                .BuildConfigurationFrom(new EndpointWithXmlSerialization(), typeof(ServerEndpoint));

            busConfig.Builder.Build<Serializers.XML.MessageSerializer>().ShouldNotBeNull();
        }

        [Test]
        public void XmlSerialization_using_a_custom_namespace_can_be_requested()
        {
            var busConfig = new ConfigurationBuilder()
                .BuildConfigurationFrom(new EndpointWithXmlSerialization(), typeof(ServerEndpoint));

            busConfig.Builder.Build<Serializers.XML.MessageSerializer>().Namespace.ShouldEqual("testnamespace");
        }

        [Test]
        public void Ordering_of_messagehandlers_can_be_specified()
        {
            var allHandlers = new ConfigurationBuilder()
                .BuildConfigurationFrom(new EndpointWithMessageHandlerOrdering(), typeof(ServerEndpoint))
                .Builder.BuildAll<IMessageHandler<IMessage>>();

            allHandlers.ElementAt(0).ShouldBeInstanceOfType(typeof (GridInterceptingMessageHandler));
            allHandlers.ElementAt(1).ShouldBeInstanceOfType(typeof(SagaMessageHandler));
        }
    }

    public class EndpointWithMessageHandlerOrdering : IConfigureThisEndpoint,As.aServer, ISpecify.MessageHandlerOrdering
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>
                .Then<SagaMessageHandler>());
        }
    }

    public class EndpointWithXmlSerialization : IConfigureThisEndpoint,ISpecify.ToUseXmlSerialization,ISpecify.XmlSerializationNamespace 
    {
        public string Namespace
        {
            get { return "testnamespace"; }
        }
    }
}