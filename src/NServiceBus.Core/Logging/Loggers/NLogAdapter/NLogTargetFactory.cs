namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// Factory to create NLog targets
    /// </summary>
    public class NLogTargetFactory
    {
        private static readonly Type ConsoleTargetType = Type.GetType("NLog.Targets.ConsoleTarget, NLog");
        private static readonly Type ColoredConsoleTargetType = Type.GetType("NLog.Targets.ColoredConsoleTarget, NLog");
        private static readonly Type FileTargetType = Type.GetType("NLog.Targets.FileTarget, NLog");

        private static readonly Type FileArchivePeriodType = Type.GetType("NLog.Targets.FileArchivePeriod, NLog");
        private static readonly Type ArchiveNumberingModeType = Type.GetType("NLog.Targets.ArchiveNumberingMode, NLog");

        private static readonly Type SimpleLayoutType = Type.GetType("NLog.Layouts.SimpleLayout, NLog");
        private static readonly Type LayoutType = Type.GetType("NLog.Layouts.Layout, NLog");

        static NLogTargetFactory()
        {
            if (ConsoleTargetType == null || ColoredConsoleTargetType == null || FileTargetType == null)
                throw new InvalidOperationException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");
        }

        public static object CreateConsoleTarget(string layout = null)
        {
            var target = Activator.CreateInstance(ConsoleTargetType);

            SetLayout(layout, target);

            return target;
        }

        public static object CreateColoredConsoleTarget(string layout = null)
        {
            dynamic target = Activator.CreateInstance(ColoredConsoleTargetType);

            target.UseDefaultRowHighlightingRules = true;

            SetLayout(layout, target);

            return target;
        }


        public static object CreateRollingFileTarget(string filename, string layout = null)
        {
            dynamic target = Activator.CreateInstance(FileTargetType);

            var archiveFilename = string.Format("{0}.{{#}}", filename);

            target.FileName = (dynamic)LayoutType.InvokeStaticMethod("FromString", filename);
            target.ArchiveFileName = (dynamic)LayoutType.InvokeStaticMethod("FromString", archiveFilename);
            target.ArchiveAboveSize = 1024 * 1024;
            target.ArchiveEvery = (dynamic)Enum.Parse(FileArchivePeriodType, "Day");
            target.ArchiveNumbering = (dynamic)Enum.Parse(ArchiveNumberingModeType, "Rolling");
            target.MaxArchiveFiles = 10;
            target.KeepFileOpen = false;

            SetLayout(layout, target);

            return target;
        }

        private static void SetLayout(string layout, dynamic target)
        {
            if (layout != null)
            {
                target.Layout= (dynamic)Activator.CreateInstance(SimpleLayoutType, layout);
            }
        }
    }
}