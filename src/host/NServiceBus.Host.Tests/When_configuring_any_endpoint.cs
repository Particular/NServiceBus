using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Sagas.Impl;
using NServiceBus.ObjectBuilder;
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

        [Test]
        public void Assemblies_can_be_specified_explicitly()
        {
            var configure = Util.Init<EndpointWithExplicitAssemblyScanning>();


            configure.Builder.Build<TestDependency>().ShouldNotBeNull();
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

    public class EndpointWithXmlSerialization : IConfigureThisEndpoint,ISpecify.ToUse.XmlSerialization,ISpecify.XmlSerializationNamespace, IDontWant.Sagas 
    {
        public string Namespace
        {
            get { return "testnamespace"; }
        }
    }
    public class EndpointWithExplicitAssemblyScanning : IConfigureThisEndpoint, 
                                                    IDontWant.Sagas,
                                                    As.aServer ,
                                                    IWantCustomInitialization,
                                                    ISpecify.AssembliesToScan

    {
        public string Namespace
        {
            get { return "testnamespace"; }
        }

        public void Init(Configure configure)
        {
            configure.Configurer.ConfigureComponent<TestDependency>(ComponentCallModelEnum.Singlecall);
        }

        public IEnumerable<Assembly> AssembliesToScan
        {
            get
            {
                //return new[] {typeof (TestDependency).Assembly};
                return new[] { Assembly.LoadFile(new FileInfo(typeof(TestDependency).Assembly.ManifestModule.Name).FullName) };
            }
        }
    }

    public class TestDependency
    {
    }


    public class EndpointWithOwnConfigSource : IConfigureThisEndpoint,ISpecify.MyOwn.ConfigurationSource, IDontWant.Sagas 
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