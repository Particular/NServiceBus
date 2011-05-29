using System;
using System.Threading;
using NUnit.Framework;

namespace NServiceBus.MasterNode.Discovery.Tests
{
    [TestFixture]
    public class When_Two_Nodes_Are_Online
    {
        [Test]
        public void They_should_identify_the_one_that_is_the_master()
        {
            Address.InitializeLocalAddress("test");

            var m = new MasterNodeManager();
            string a = null, b = null;

            m.YieldMasterNode("test@A", true, s => a = s);
            m.YieldMasterNode("test@B", false, s => b = s);

            Thread.Sleep(MasterNodeManager.presenceInterval + TimeSpan.FromSeconds(1));

            Assert.AreEqual(a, b);
            Assert.AreEqual("test@A", a);
        }
    }
}
