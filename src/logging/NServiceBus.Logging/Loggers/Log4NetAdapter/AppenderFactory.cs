using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    public class AppenderFactory
    {
        private static readonly Type LevelType = Type.GetType("log4net.Core.Level, log4net");
        private static readonly Type ConsoleAppenderType = Type.GetType("log4net.Appender.ConsoleAppender, log4net");
        private static readonly Type ColoredConsoleAppenderType = Type.GetType("log4net.Appender.ColoredConsoleAppender, log4net");
        private static readonly Type RollingFileAppenderType = Type.GetType("log4net.Appender.RollingFileAppender, log4net");
        private static readonly Type LevelColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+LevelColors, log4net");
        private static readonly Type ColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+Colors, log4net");

        private static readonly Type RollingModeType = Type.GetType("log4net.Appender.RollingFileAppender+RollingMode, log4net");
        private static readonly Type MinimalLockType = Type.GetType("log4net.Appender.FileAppender+MinimalLock, log4net");

        static AppenderFactory()
        {
            if (ColoredConsoleAppenderType == null)
                throw new InvalidOperationException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");
        }

        public static object CreateConsoleAppender(string level)
        {
            var appender = Activator.CreateInstance(ConsoleAppenderType);

            if (level != null)
                SetThreshold(appender, level);

            return appender;
        }

        public static object CreateColoredConsoleAppender(string level)
        {
            var appender = Activator.CreateInstance(ColoredConsoleAppenderType);

            if (level != null)
                SetThreshold(appender, level);

            AddMapping(appender, "Debug", "White");
            AddMapping(appender, "Info", "Green");
            AddMapping(appender, "Warn", "Yellow", "HighIntensity");
            AddMapping(appender, "Error", "Red", "HighIntensity");

            return appender;
        }

        public static object CreateRollingFileAppender(string level, string filename)
        {
            var appender = Activator.CreateInstance(RollingFileAppenderType);

            if (level != null)
                SetThreshold(appender, level);

            appender.SetProperty("CountDirection", 1);
            appender.SetProperty("DatePattern", "yyyy-MM-dd");

            appender.SetProperty("RollingStyle", Enum.Parse(RollingModeType, "Composite"));
            appender.SetProperty("MaxFileSize", 1024*1024);
            appender.SetProperty("MaxSizeRollBackups", 10);
            appender.SetProperty("LockingModel", Activator.CreateInstance(MinimalLockType));
            appender.SetProperty("StaticLogFileName", true);
            appender.SetProperty("File", filename);
            appender.SetProperty("AppendToFile", true);

            return appender;
        }

        private static void AddMapping(object appender, string level, string color1, string color2 = null)
        {
            var levelColors = Activator.CreateInstance(LevelColorsType);

            levelColors.SetProperty("Level", LevelType.GetStaticField(level));
            levelColors.SetProperty("ForeColor", GetColors(color1, color2));

            appender.InvokeMethod("AddMapping", levelColors);
        }

        private static object GetColors(string color1, string color2)
        {
            var colorsValue = (int)Enum.Parse(ColorsType, color1);

            if (color2 != null)
                colorsValue |= (int)Enum.Parse(ColorsType, color2);

            return Enum.ToObject(ColorsType, colorsValue);
        }

        public static void SetThreshold(object appender, string threshold)
        {
            appender.SetProperty("Threshold", LevelType.GetStaticField(threshold));
        }

        public static object GetThreshold(object appender)
        {
            return appender.GetProperty("Threshold");
        }
    }
}