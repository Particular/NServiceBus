namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Logging;

    class ContextAppenderFactory : ILoggerFactory
    {
        static ScenarioContext context;
        

        /// <summary>
        /// Because ILoggerFactory interface methods are only used in a static context. This is the only way to set the currently executing context.
        /// </summary>
        /// <param name="newContext">The new context to be set</param>
        public static void SetContext(ScenarioContext newContext)
        {
            context = newContext;
        }

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            LogLevel level;
            if (!context.LogLevels.TryGetValue(name, out level))
            {
                level = LogLevel.Info;
            }
            return new ContextAppender(name, level, () => context);
        }
    }
}