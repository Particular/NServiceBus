using System.ServiceModel;
using System.ServiceModel.Channels;
using NServiceBus.Host.Internal;
using NUnit.Framework;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class WcfManagerTests
    {
        [Test,Explicit("Run this test manually")]
        public void StartServiceHost()
        {
            var manager = new WcfManager(new[] { typeof(WcfManagerTests).Assembly },new EndpointConfig());

            manager.Startup();

        }
    }

    public class EndpointConfig:IConfigureThisEndpoint,ISpecifyDefaultWcfBinding
    {
        public Binding SpecifyBinding()
        {
            return new WSHttpBinding();
        }
    }

    public class TestWcfService:WcfService<RequestMessage,ResponseMessage>
    {
        
    }

    public class ResponseMessage

    {
    }

    public class RequestMessage : IMessage
    {
    }
} 