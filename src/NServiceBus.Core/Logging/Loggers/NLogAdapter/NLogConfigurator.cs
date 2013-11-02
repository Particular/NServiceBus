namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    using System;
    using System.Linq;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class NLogConfigurator
    {
        private static readonly Type TargetType = Type.GetType("NLog.Targets.Target, NLog");
        private static readonly Type LogLevelType = Type.GetType("NLog.LogLevel, NLog");
        private static readonly Type LoggingConfigurationType = Type.GetType("NLog.Config.LoggingConfiguration, NLog");
        private static readonly Type LogManagerType = Type.GetType("NLog.LogManager, NLog");
        private static readonly Type LoggingRuleType = Type.GetType("NLog.Config.LoggingRule, NLog");

        public static bool NLogExists
        {
            get { return Type.GetType("NLog.LogManager, NLog") != null; }
        }

        public static void Configure(object targetForNServiceBusToLogTo, string levelForNServiceBusToLogWith = null)
        {
            if (targetForNServiceBusToLogTo == null)
            {
                throw new ArgumentNullException("targetForNServiceBusToLogTo");
            }
            Configure(new[] { targetForNServiceBusToLogTo }, levelForNServiceBusToLogWith);
        }

        public static void Configure(object[] targetsForNServiceBusToLogTo, string levelForNServiceBusToLogWith = null)
        {
            EnsureNLogExists();

            if (!targetsForNServiceBusToLogTo.All(x => TargetType.IsInstanceOfType(x)))
                throw new ArgumentException("The objects provided must inherit from NLog.Targets.Target.");

            
            dynamic loggingConfiguration = LogManagerType.GetStaticProperty("Configuration");
            if (loggingConfiguration == null)
            {
                loggingConfiguration = Activator.CreateInstance(LoggingConfigurationType);
            }
            foreach (dynamic target in targetsForNServiceBusToLogTo)
            {
                //TODO:check if target is owned by another config
                if (target.Name == null)
                {
                    var name = target.GetType().Name;
                    loggingConfiguration.AddTarget(name, target);
                }
            }

            var logLevel = LogLevelType.GetStaticField(levelForNServiceBusToLogWith ?? "Info", true);
            dynamic loggingRule = Activator.CreateInstance(LoggingRuleType, "*", logLevel, targetsForNServiceBusToLogTo.First());

            foreach (dynamic target in targetsForNServiceBusToLogTo.Skip(1))
            {
                loggingRule.Targets.Add(target);
            }

            loggingConfiguration.LoggingRules.Add(loggingRule);
            LogManagerType.SetStaticProperty("Configuration", (object)loggingConfiguration);
            Configure();

        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Configure()
        {
            EnsureNLogExists();

            LogManager.LoggerFactory = new NLogLoggerFactory();
        }

        private static void EnsureNLogExists()
        {
            if (!NLogExists)
                throw new LoggingLibraryException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");
        }
    }
}