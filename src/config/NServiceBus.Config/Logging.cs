using System.Collections.Specialized;
using Common.Logging;

namespace NServiceBus
{
    /// <summary>
    /// Class containing extension method to allow users to use Log4Net for logging
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Use Log4Net for logging.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure Log4Net(this Configure config)
        {
            UseLog4Net();

            return config;
        }

        /// <summary>
        /// Use Log4Net for logging.
        /// </summary>
        public static void UseLog4Net()
        {
            var props = new NameValueCollection();
            props["configType"] = "EXTERNAL";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);
        }
    }
}
