using NServiceBus;
using log4net.Appender;
using log4net.Layout;
using log4net.Core;

namespace MyClient
{
    public class LoggingConfiguration : IConfigureLoggingForProfile<Production>
    {
        public void Configure(IConfigureThisEndpoint specifier)
        {
            SetLoggingLibrary.Log4Net();

            var appender = new ColoredConsoleAppender()
            {
                Layout = new SimpleLayout(),
                Threshold = Level.Debug
            };

            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Debug,
                    ForeColor = ColoredConsoleAppender.Colors.Purple
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Info,
                    ForeColor = ColoredConsoleAppender.Colors.Green
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Warn,
                    ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Error,
                    ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
                });

            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);
        }
    }
}