using System;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
    using Loggers.NLogAdapter;

    [TestFixture]
    public class TargetFactoryTests
    {
        [SetUp]
        public void Setup()
        {
            NLog.LogManager.Configuration = null;
        }

        [Test]
        public void Can_create_ConsoleTarget()
        {
            var target = NLogTargetFactory.CreateColoredConsoleTarget();

            NLogConfigurator.Configure(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing console target");
        }

        [Test]
        public void Can_create_ColoredConsoleTarget()
        {
            var target = NLogTargetFactory.CreateColoredConsoleTarget("${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}");

            NLogConfigurator.Configure(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing colored console target");
            NLog.LogManager.GetCurrentClassLogger().DebugException("Testing colored console target", new Exception());
        }

        [Test]
        public void Can_create_RollingFileTarget()
        {
            var target = NLogTargetFactory.CreateRollingFileTarget("nlog.txt");

            NLogConfigurator.Configure(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing rolling file target");
        }
    }
}