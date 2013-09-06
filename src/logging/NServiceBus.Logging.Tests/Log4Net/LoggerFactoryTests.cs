namespace NServiceBus.Logging.Tests.Log4Net
{
    using log4net.Appender;
    using log4net.Core;
    using Loggers.Log4NetAdapter;
    using NUnit.Framework;

    [TestFixture]
    public class LoggerFactoryTests : BaseLoggerFactoryTests
    {

        [Test]
        public void Test()
        {
            var loggerFactory = new Log4NetLoggerFactory();

            global::log4net.LogManager.ResetConfiguration();
            Log4NetConfigurator.Configure(new ConsoleAppender { Threshold = Level.All });

            var log = loggerFactory.GetLogger("Test");

            Assert.IsInstanceOf<Log4NetLogger>(log);

            CallAllMethodsOnLog(log);

            log = loggerFactory.GetLogger(typeof(LoggerFactoryTests));

            CallAllMethodsOnLog(log);
        }
    }
}
