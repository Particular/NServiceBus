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

            return target;
        }

        public static object CreateRollingFileTarget(string filename)
        {
            var target = Activator.CreateInstance(FileTargetType);

            string archiveFilename = string.Format("{0}.{{#}}", filename);

            target.SetProperty("FileName", LayoutType.InvokeStaticMethod("FromString", filename));
            target.SetProperty("ArchiveFileName", LayoutType.InvokeStaticMethod("FromString", archiveFilename));
            target.SetProperty("ArchiveAboveSize", 1024 * 1024);
            target.SetProperty("ArchiveEvery", Enum.Parse(FileArchivePeriodType, "Day"));
            target.SetProperty("ArchiveNumbering", Enum.Parse(ArchiveNumberingModeType, "Rolling"));
            target.SetProperty("MaxArchiveFiles", 10);
            target.SetProperty("KeepFileOpen", false);

            return target;
        }
    }
}