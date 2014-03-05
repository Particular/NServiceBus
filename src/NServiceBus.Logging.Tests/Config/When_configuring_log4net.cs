﻿namespace NServiceBus.Logging.Config.Tests
{
    using log4net;
    using log4net.Appender;
    using log4net.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_log4net
    {
        [SetUp]
        public void Setup()
        {
            LogManager.ResetConfiguration();
        }

        [Test]
        public void Default_ConsoleLogger()
        {
            Configure.With().Log4Net();

            LogManager.GetLogger("Test").Debug("Testing Debug");
        }

        [Test]
        public void Custom_Logger()
        {
            Configure.With().Log4Net<ConsoleAppender>(x => { });

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
        }

        [Test]
        public void Custom_Logger_with_configured_threshold()
        {
            Configure.With().Log4Net<ConsoleAppender>(x => { x.Threshold = Level.Error; });

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
            LogManager.GetLogger("Test").Error("Testing Error");
        }
    }
}
