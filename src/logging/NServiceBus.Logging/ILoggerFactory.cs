using System;

namespace NServiceBus.Logging
{
    /// <summary>
  /// 
  /// </summary>
  public interface ILoggerFactory
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    ILog GetLogger(Type type);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ILog GetLogger(string name);
  }
}