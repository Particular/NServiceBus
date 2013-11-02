﻿namespace NServiceBus.Logging.Log4NetBridge.Tests
{
    using Loggers;
    using NUnit.Framework;

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
