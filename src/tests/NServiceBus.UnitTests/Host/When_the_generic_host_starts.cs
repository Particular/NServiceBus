using NUnit.Framework;

namespace NServiceBus.UnitTests.Host
{
    [TestFixture]
    public class When_the_generic_host_starts
    {
        //private GenericHost host;

        [SetUp]
        public void SetUp()
        {
            //host = new GenericHost(typeof (TestEndpoint));

            //host.Start();
        }

        [Test]
        public void It_should_get_the_bus_configuration()
        {
            
        }
    }

    //public class TestEndpoint : IMessageEndpoint
    //{

    //    public void OnStart()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void OnStop()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}