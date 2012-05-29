using NServiceBus.Logging.Loggers;
using NUnit.Framework;

namespace NServiceBus.Logging.Log4NetBridge.Tests
{
    [TestFixture]
    public class DummyTests
    {
        [Test]
        public void Test()
        {
            LogManager.LoggerFactory = new ConsoleLoggerFactory();

            LogManager.GetLogger("Testing").Debug("Hello");

            new ConfigureInternalLog4NetBridge().Init();

            log4net.LogManager.GetLogger("BridgedLogger").Debug("Hello from log4net");
        }

    }
}
