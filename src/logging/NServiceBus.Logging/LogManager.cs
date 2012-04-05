using System;

namespace NServiceBus.Logging
{
  /// <summary>
  /// 
  /// </summary>
  public class LogManager
  {
    /// <summary>
    /// 
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ILog GetLogger(Type type)
    {
      if (LoggerFactory == null)
        return new ConsoleLogger();

      return LoggerFactory.GetLogger(type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static ILog GetLogger(string name)
    {
      if (LoggerFactory == null)
        return new ConsoleLogger();

      return LoggerFactory.GetLogger(name);
    }
  }
}