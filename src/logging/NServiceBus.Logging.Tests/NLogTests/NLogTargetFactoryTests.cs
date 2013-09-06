namespace NServiceBus.Logging.Tests.NLogTests
{
    using Loggers.NLogAdapter;
    using NUnit.Framework;

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