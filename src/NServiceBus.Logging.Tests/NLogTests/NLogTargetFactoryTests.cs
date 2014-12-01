using NServiceBus.Logging.Loggers.NLogAdapter;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
    [TestFixture]
    public class NLogTargetFactoryTests
    {

        [Test]
        public void TestDynamicConstruction()
        {
            NLogTargetFactory.CreateColoredConsoleTarget();
            NLogTargetFactory.CreateConsoleTarget();
            NLogTargetFactory.CreateRollingFileTarget("foo");
        }
    }
}