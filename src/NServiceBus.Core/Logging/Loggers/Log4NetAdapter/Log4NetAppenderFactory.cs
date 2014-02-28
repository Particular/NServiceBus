namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    using System;
    using Internal;

    public class Log4NetAppenderFactory
    {
        private static readonly Type LevelType = Type.GetType("log4net.Core.Level, log4net");
        private static readonly Type ConsoleAppenderType = Type.GetType("log4net.Appender.ConsoleAppender, log4net");
        private static readonly Type ColoredConsoleAppenderType = Type.GetType("log4net.Appender.ColoredConsoleAppender, log4net");
        private static readonly Type RollingFileAppenderType = Type.GetType("log4net.Appender.RollingFileAppender, log4net");
        private static readonly Type LevelColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+LevelColors, log4net");
        private static readonly Type ColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+Colors, log4net");

        private static readonly Type RollingModeType = Type.GetType("log4net.Appender.RollingFileAppender+RollingMode, log4net");
        private static readonly Type MinimalLockType = Type.GetType("log4net.Appender.FileAppender+MinimalLock, log4net");

        static Log4NetAppenderFactory()
        {
            if (ColoredConsoleAppenderType == null)
                throw new InvalidOperationException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");
        }

        public static object CreateConsoleAppender(string level)
        {
            dynamic appender = Activator.CreateInstance(ConsoleAppenderType);

            if (level != null)
                appender.Threshold = ConvertToLogLevel(level);

            return appender;
        }

        public static object CreateColoredConsoleAppender(string level)
        {
            dynamic appender = Activator.CreateInstance(ColoredConsoleAppenderType);

            if (level != null)
                appender.Threshold = ConvertToLogLevel(level);

            AddMapping(appender, "Debug", "White");
            AddMapping(appender, "Info", "Green");
            AddMapping(appender, "Warn", "Yellow", "HighIntensity");
            AddMapping(appender, "Error", "Red", "HighIntensity");

            return appender;
        }

        public static object CreateRollingFileAppender(string level, string filename)
        {
            dynamic appender = Activator.CreateInstance(RollingFileAppenderType);

            if (level != null)
                appender.Threshold = ConvertToLogLevel(level);

            appender.CountDirection = 1;
            appender.DatePattern = "yyyy-MM-dd";

            // ReSharper disable once RedundantCast
            appender.RollingStyle = (dynamic)Enum.Parse(RollingModeType, "Composite");
            appender.MaxFileSize = 1024*1024;
            appender.MaxSizeRollBackups = 10;
            // ReSharper disable once RedundantCast
            appender.LockingModel = (dynamic)Activator.CreateInstance(MinimalLockType);
            appender.StaticLogFileName = true;
            appender.File =filename;
            appender.AppendToFile = true;

            return appender;
        }

        internal static dynamic ConvertToLogLevel(string level)
        {
            return LevelType.GetStaticField(level, true);
        }

        private static void AddMapping(dynamic appender, string level, string color1, string color2 = null)
        {
            dynamic levelColors = Activator.CreateInstance(LevelColorsType);

            levelColors.Level = ConvertToLogLevel(level);
            levelColors.ForeColor = GetColors(color1, color2);

            appender.AddMapping(levelColors);
        }

        private static dynamic GetColors(string color1, string color2)
        {
            var colorsValue = (int)Enum.Parse(ColorsType, color1);

            if (color2 != null)
                colorsValue |= (int)Enum.Parse(ColorsType, color2);

            return Enum.ToObject(ColorsType, colorsValue);
        }

    }
}