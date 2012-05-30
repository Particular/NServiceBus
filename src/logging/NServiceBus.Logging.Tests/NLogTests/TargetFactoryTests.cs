using NServiceBus.Logging.Loggers.NLogAdapter;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
    [TestFixture]
    public class TargetFactoryTests
    {
        [Test]
        public void Can_create_ConsoleTarget()
        {
            var appender = TargetFactory.CreateColoredConsoleTarget();
        }

        [Test]
        public void Can_create_ColoredConsoleTarget()
        {
            var appender = TargetFactory.CreateColoredConsoleTarget();
        }

        [Test]
        public void Can_create_RollingFileTarget()
        {
            var appender = TargetFactory.CreateRollingFileTarget("logfile");
        }
    }
}