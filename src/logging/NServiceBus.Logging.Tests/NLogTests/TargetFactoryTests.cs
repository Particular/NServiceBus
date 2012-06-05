using NServiceBus.Logging.Loggers.NLogAdapter;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
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
            var target = TargetFactory.CreateColoredConsoleTarget();

            Configurator.Basic(target, "Debug");

            NLog.LogManager.GetCurrentClassLogger().Debug("Testing colored console target");
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