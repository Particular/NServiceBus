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
            var target = TargetFactory.CreateColoredConsoleTarget();

            Configurator.Basic(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing console target");
        }

        [Test]
        public void Can_create_ColoredConsoleTarget()
        {
            var target = TargetFactory.CreateColoredConsoleTarget("${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}");

            Configurator.Basic(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing colored console target");
            NLog.LogManager.GetCurrentClassLogger().DebugException("Testing colored console target", new Exception());
        }

        [Test]
        public void Can_create_RollingFileTarget()
        {
            var target = TargetFactory.CreateRollingFileTarget("nlog.txt");

            Configurator.Basic(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing rolling file target");
        }
    }
}