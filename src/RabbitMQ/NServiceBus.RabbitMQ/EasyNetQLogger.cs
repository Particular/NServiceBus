namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using Logging;

    public class EasyNetQLogger : EasyNetQ.IEasyNetQLogger
    {
        readonly ILog _logger;

        public EasyNetQLogger(ILog logger) {
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }
            _logger = logger;
        }

        public void DebugWrite(string format, params object[] args) {
            _logger.Debug(string.Format(format,args));
        }

        public void InfoWrite(string format, params object[] args) {
            _logger.Info(string.Format(format,args));
        }

        public void ErrorWrite(string format, params object[] args) {
            _logger.Error(string.Format(format,args));
        }

        public void ErrorWrite(Exception exception) {
            _logger.Error(string.Empty,exception);
        }
    }
}