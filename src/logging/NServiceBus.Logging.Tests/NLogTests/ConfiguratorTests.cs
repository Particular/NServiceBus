namespace NServiceBus.Logging.Tests.NLogTests
{
    using Loggers.NLogAdapter;
    using NLog.Layouts;
    using NLog.Targets;
    using NUnit.Framework;

    [TestFixture]
    public class ConfiguratorTests
    {

        [Test]
        public void Threshold_should_be_All()
        {
            new ColoredConsoleTarget() {UseDefaultRowHighlightingRules = true};

            var filename = "logfile";

            NLog.Config.SimpleConfigurator.ConfigureForConsoleLogging();

            //LoggingConfiguration config = new LoggingConfiguration();
            //LoggingRule rule = new LoggingRule("*", minLevel, consoleTarget);
            //config.LoggingRules.Add(rule);
            //NLog.LogManager.Configuration = config;


            new NLog.Config.LoggingConfiguration();

            new FileTarget()
                {
                    FileName = Layout.FromString(filename),
                    ArchiveFileName = string.Format("{0}.{{#}}", filename),
                    ArchiveAboveSize = 1024 * 1024,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveNumbering = ArchiveNumberingMode.Rolling,
                    MaxArchiveFiles = 10,
                    KeepFileOpen = false
                };

            NLogConfigurator.Configure(new ConsoleTarget(), "Debug");

            LogManager.GetLogger("Test").Debug("Testing Debug");
        }

        [Test]
        public void Threshold_default_should_be_Info()
        {
            NLogConfigurator.Configure(new ConsoleTarget());

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
        }

        [Test]
        public void Threshold_default_should_be_Error()
        {
            NLogConfigurator.Configure(new ConsoleTarget(), "Error");

            LogManager.GetLogger("Test").Debug("Testing Debug");
            LogManager.GetLogger("Test").Info("Testing Info");
            LogManager.GetLogger("Test").Error("Testing Error");
        }

        [Test]
        public void Can_configure_2_targets()
        {
            NLogConfigurator.Configure(new Target[] { new ConsoleTarget(), new ColoredConsoleTarget()});

            var loggingConfiguration = NLog.LogManager.Configuration;
            Assert.AreEqual(2, loggingConfiguration.LoggingRules[0].Targets.Count);
        }
    }
}