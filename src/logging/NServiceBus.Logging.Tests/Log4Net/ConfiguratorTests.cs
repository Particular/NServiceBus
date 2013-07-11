using NUnit.Framework;
using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Logging.Tests.Log4Net
{
    using Loggers.Log4NetAdapter;

    [TestFixture]
    public class ConfiguratorTests
    {
        [SetUp]
        public void Setup()
        {
            log4net.LogManager.ResetConfiguration();
        }

        [Test]
        public void Threshold_should_be_All()
        {
            Log4NetConfigurator.Configure(new ConsoleAppender { Threshold = Level.All });

            LogManager.GetLogger("Test").Debug("Testing Debug");
        }

        [Test]
        public void Threshold_default_should_be_Info()
        {
            Log4NetConfigurator.Configure(new ConsoleAppender());

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
        }

        [Test]
        public void Threshold_default_should_be_Error()
        {
            Log4NetConfigurator.Configure(new ConsoleAppender { Threshold = Level.Error });

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
            LogManager.GetLogger("Test").Error("Testing Error");
        }
    }
}