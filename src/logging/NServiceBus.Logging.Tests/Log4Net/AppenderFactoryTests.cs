using NServiceBus.Logging.Log4Net;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.Log4Net
{
    [TestFixture]
    public class AppenderFactoryTests
    {
        [Test]
        public void Can_create_ColoredConsoleAppender()
        {
            var appender = AppenderFactory.CreateColoredConsoleAppender("Debug");
        }

        [Test]
        public void Can_create_RollingFileAppender()
        {
            var appender = AppenderFactory.CreateRollingFileAppender("Debug", "logfile");
        }
    }
}