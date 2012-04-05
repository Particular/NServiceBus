using System;

namespace NServiceBus.Logging.Log4Net
{
  /// <summary>
  /// 
  /// </summary>
  public class LoggerFactory : ILoggerFactory
  {
    public ILog GetLogger(Type type)
    {
      return new Log(log4net.LogManager.GetLogger(type));
    }

    public ILog GetLogger(string name)
    {
      return new Log(log4net.LogManager.GetLogger(name));
    }
  }
}