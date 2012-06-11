using System;
using System.Linq;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    /// <summary>
    /// 
    /// </summary>
    public class Configurator
    {
        private static readonly Type TargetType = Type.GetType("NLog.Targets.Target, NLog");
        private static readonly Type LogLevelType = Type.GetType("NLog.LogLevel, NLog");
        private static readonly Type LoggingConfigurationType = Type.GetType("NLog.Config.LoggingConfiguration, NLog");
        private static readonly Type LogManagerType = Type.GetType("NLog.LogManager, NLog");
        private static readonly Type LoggingRuleType = Type.GetType("NLog.Config.LoggingRule, NLog");

        public static void Basic(object target, string level = null)
        {
            Basic(new [] { target}, level);
        }

        public static void Basic(object[] targets, string level = null)
        {
            EnsureNLogExists();

            if (!targets.All(x => TargetType.IsInstanceOfType(x)))
                throw new ArgumentException("The objects provided must inherit from NLog.Targets.Target.");

            Basic();
            
            var loggingConfiguration = Activator.CreateInstance(LoggingConfigurationType);

            object loggingRule = Activator.CreateInstance(LoggingRuleType, "*", LogLevelType.GetStaticField(level ?? "Info"), targets.First());

            foreach (var target in targets.Skip(1))
            {
                loggingRule.GetProperty("Targets")
                    .InvokeMethod("Add", target);
            }

            loggingConfiguration
                .GetProperty("LoggingRules")
                .InvokeMethod("Add", loggingRule);

            LogManagerType.SetStaticProperty("Configuration", loggingConfiguration);
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Basic()
        {
            EnsureNLogExists();

            LogManager.LoggerFactory = new LoggerFactory();
        }

        private static void EnsureNLogExists()
        {
            if (LogManagerType == null)
                throw new LoggingLibraryException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");
        }
    }
}