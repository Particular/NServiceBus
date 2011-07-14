using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the lite profile.
    /// </summary>
    public class LiteLoggingHandler : IConfigureLoggingForProfile<Lite>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            SetLoggingLibrary.Log4Net<ColoredConsoleAppender>(null, 
                a =>
                {
                    PrepareColors(a);
                    a.Threshold = Level.Info;
                }
            );
        }

        
        ///<summary>
        /// Sets default colors for a ColredConsoleAppender
        ///</summary>
        ///<param name="a"></param>
        public static void PrepareColors(ColoredConsoleAppender a)
        {
            a.AddMapping(
                new ColoredConsoleAppender.LevelColors
                    {
                        Level = Level.Debug,
                        ForeColor = ColoredConsoleAppender.Colors.White
                    });
            a.AddMapping(
                new ColoredConsoleAppender.LevelColors
                    {
                        Level = Level.Info,
                        ForeColor = ColoredConsoleAppender.Colors.Green
                    });
            a.AddMapping(
                new ColoredConsoleAppender.LevelColors
                    {
                        Level = Level.Warn,
                        ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity
                    });
            a.AddMapping(
                new ColoredConsoleAppender.LevelColors
                    {
                        Level = Level.Error,
                        ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
                    });
        }
    }
}