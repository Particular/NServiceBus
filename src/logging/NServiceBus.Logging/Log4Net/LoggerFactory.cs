using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Log4Net
{
  /// <summary>
  /// 
  /// </summary>
  public class LoggerFactory : ILoggerFactory
  {
    private readonly Func<Type, object> getLoggerByTypeDelegate;
    private readonly Func<String, object> getLoggerByStringDelegate;

    public LoggerFactory()
    {
      var logManagerType = Type.GetType("log4net.LogManager, log4net");

      if (logManagerType == null)
        throw new InvalidOperationException("Could not find log4net. There must be a log4net assembly present in the executable directory.");

      getLoggerByTypeDelegate = logManagerType.GetStaticFunctionDelegate<Type, object>("GetLogger");
      getLoggerByStringDelegate = logManagerType.GetStaticFunctionDelegate<String, object>("GetLogger");
    }

    public ILog GetLogger(Type type)
    {
      return new Log(getLoggerByTypeDelegate(type));
    }

    public ILog GetLogger(string name)
    {
      return new Log(getLoggerByStringDelegate(name));
    }
  }
}