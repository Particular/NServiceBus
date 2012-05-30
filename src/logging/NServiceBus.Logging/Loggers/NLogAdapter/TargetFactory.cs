using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    /// <summary>
    /// Factory to create NLog targets
    /// </summary>
    public class TargetFactory
    {
        private static readonly Type ConsoleTargetType = Type.GetType("NLog.Targets.ConsoleTarget, NLog");
        private static readonly Type ColoredConsoleTargetType = Type.GetType("NLog.Targets.ColoredConsoleTarget, NLog");
        private static readonly Type FileTargetType = Type.GetType("NLog.Targets.FileTarget, NLog");

        private static readonly Type FileArchivePeriodType = Type.GetType("NLog.Targets.FileArchivePeriod, NLog");
        private static readonly Type ArchiveNumberingModeType = Type.GetType("NLog.Targets.ArchiveNumberingMode, NLog");

        private static readonly Type LayoutType = Type.GetType("NLog.Layouts.Layout, NLog");

        //private static readonly Type LevelColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+LevelColors, NLog");
        //private static readonly Type ColorsType = Type.GetType("log4net.Appender.ColoredConsoleAppender+Colors, NLog");


        static TargetFactory()
        {
            if (ConsoleTargetType == null || ColoredConsoleTargetType == null || FileTargetType == null)
                throw new InvalidOperationException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");
        }

        public static object CreateConsoleTarget()
        {
            return Activator.CreateInstance(ConsoleTargetType);
        }

        public static object CreateColoredConsoleTarget()
        {
            var target = Activator.CreateInstance(ColoredConsoleTargetType);

            target.SetProperty("UseDefaultRowHighlightingRules", true);

            //if (level != null)
            //    SetThreshold(appender, level);

            //AddMapping(appender, "Debug", "White");
            //AddMapping(appender, "Info", "Green");
            //AddMapping(appender, "Warn", "Yellow", "HighIntensity");
            //AddMapping(appender, "Error", "Red", "HighIntensity");

            return target;
        }

        public static object CreateRollingFileTarget(string filename)
        {
            var target = Activator.CreateInstance(FileTargetType);
            string archiveFilename = string.Format("{0}.{{#}}", filename);

            target.SetProperty("FileName", LayoutType.InvokeStaticMethod("FromString", new Type[] { typeof(string) }, new object[] { filename }));
            target.SetProperty("ArchiveFileName", LayoutType.InvokeStaticMethod("FromString", new Type[] { typeof(string) }, new object[] { archiveFilename }));
            target.SetProperty("ArchiveAboveSize", 1024 * 1024);
            target.SetProperty("ArchiveEvery", Enum.Parse(FileArchivePeriodType, "Day"));
            target.SetProperty("ArchiveNumbering", Enum.Parse(ArchiveNumberingModeType, "Rolling"));
            target.SetProperty("MaxArchiveFiles", 10);
            target.SetProperty("KeepFileOpen", false);

            return target;
        }

        //private static void AddMapping(object appender, string level, string color1, string color2 = null)
        //{
        //    var levelColors = Activator.CreateInstance(LevelColorsType);

        //    levelColors.SetProperty("Level", LevelType.GetStaticField(level));
        //    levelColors.SetProperty("ForeColor", GetColors(color1, color2));

        //    appender.InvokeMethod("AddMapping", levelColors);
        //}

        //private static object GetColors(string color1, string color2)
        //{
        //    var colorsValue = (int)Enum.Parse(ColorsType, color1);

        //    if (color2 != null)
        //        colorsValue |= (int)Enum.Parse(ColorsType, color2);

        //    return Enum.ToObject(ColorsType, colorsValue);
        //}

        //public static void SetLevel(object appender, string level)
        //{
        //    appender.SetProperty("Threshold", LevelType.GetStaticField(threshold));
        //}

        //public static object GetThreshold(object appender)
        //{
        //    return appender.GetProperty("Threshold");
        //}
    }
}