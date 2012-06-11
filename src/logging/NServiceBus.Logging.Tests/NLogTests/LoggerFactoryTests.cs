using NLog;
using NServiceBus.Logging.Loggers.NLogAdapter;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
    [TestFixture]
    public class LoggerFactoryTests : BaseLoggerFactoryTests
    {

        [Test]
        public void Test()
        {
            global::NLog.Config.SimpleConfigurator.ConfigureForConsoleLogging(LogLevel.Trace);

            var loggerFactory = new LoggerFactory();

            var log = loggerFactory.GetLogger("Test");

            Assert.IsInstanceOf<Log>(log);

            CallAllMethodsOnLog(log);

            log = loggerFactory.GetLogger(typeof(LoggerFactoryTests));

            CallAllMethodsOnLog(log);
        }
    }
}
