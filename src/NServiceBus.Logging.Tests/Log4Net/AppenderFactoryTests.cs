namespace NServiceBus.Logging.Tests.Log4Net
{
    using Loggers.Log4NetAdapter;
    using NUnit.Framework;

    [TestFixture]
    public class AppenderFactoryTests
    {
        [Test]
        public void Can_create_ColoredConsoleAppender()
        {
            var appender = Log4NetAppenderFactory.CreateColoredConsoleAppender("Debug");
            Assert.IsNotNull(appender);
        }

        [Test]
        public void Can_create_RollingFileAppender()
        {
            var appender = Log4NetAppenderFactory.CreateRollingFileAppender("Debug", "logfile");
            Assert.IsNotNull(appender);
        }
    }
}