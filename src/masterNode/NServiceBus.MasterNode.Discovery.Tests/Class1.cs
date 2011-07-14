using System;
using System.Threading;
using NUnit.Framework;

namespace NServiceBus.MasterNode.Discovery.Tests
{
    [TestFixture]
    public class When_Two_Nodes_Are_Online
    {
        [Test]
        [Ignore("Requires elevated priveleges to run.")]
        public void They_should_identify_the_one_that_is_the_master()
        {
            Address.InitializeLocalAddress("test");

            var m = new MasterNodeManager();
            
            string a = null, b = null;

            MasterNodeManager.Init("test@a", true, s => a = s);
            MasterNodeManager.Init("test@b", false, s => b = s);

            Thread.Sleep(MasterNodeManager.presenceInterval + TimeSpan.FromSeconds(1));

            Assert.AreEqual(a, b);
            Assert.AreEqual("test@a", a);
        }
    }
}
