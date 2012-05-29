using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Logging.Log4NetBridge
{
    internal class Log4NetBridgeAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            // This might be slow but it should not be an issue since neither Topshelf or Rhino.Licensing logs that much.
            ILog Log = LogManager.GetLogger(loggingEvent.LoggerName);

            if (loggingEvent.Level == Level.Debug)
                Log.Debug(loggingEvent.RenderedMessage, loggingEvent.ExceptionObject);
            if (loggingEvent.Level == Level.Info)
                Log.Info(loggingEvent.RenderedMessage, loggingEvent.ExceptionObject);
            if (loggingEvent.Level == Level.Warn)
                Log.Warn(loggingEvent.RenderedMessage, loggingEvent.ExceptionObject);
            if (loggingEvent.Level == Level.Error)
                Log.Error(loggingEvent.RenderedMessage, loggingEvent.ExceptionObject);
            if (loggingEvent.Level == Level.Fatal)
                Log.Fatal(loggingEvent.RenderedMessage, loggingEvent.ExceptionObject);
        }
    }
}