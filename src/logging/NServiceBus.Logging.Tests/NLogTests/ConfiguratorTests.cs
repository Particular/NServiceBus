using NLog.Layouts;
using NLog.Targets;
using NServiceBus.Logging.Loggers.NLogAdapter;
using NUnit.Framework;

namespace NServiceBus.Logging.Tests.NLogTests
{
    [TestFixture]
    public class ConfiguratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Threshold_should_be_All()
        {
            new NLog.Targets.ColoredConsoleTarget() {UseDefaultRowHighlightingRules = true};

            var filename = "logfile";

            new NLog.Targets.FileTarget()
                {
                    FileName = Layout.FromString(filename),
                    ArchiveFileName = string.Format("{0}.{{#}}", filename),
                    ArchiveAboveSize = 1024 * 1024,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveNumbering = ArchiveNumberingMode.Rolling,
                    MaxArchiveFiles = 10,
                    KeepFileOpen = false
                };

            Configurator.Basic(new NLog.Targets.ConsoleTarget(), "Debug");

            LogManager.GetLogger("Test").Debug("Testing Debug");
        }

        [Test]
        public void Threshold_default_should_be_Info()
        {
            Configurator.Basic(new NLog.Targets.ConsoleTarget());

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
        }

        [Test]
        public void Threshold_default_should_be_Error()
        {
            Configurator.Basic(new NLog.Targets.ConsoleTarget(), "Error");

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
            LogManager.GetLogger("Test").Error("Testing Error");
        }
    }
}