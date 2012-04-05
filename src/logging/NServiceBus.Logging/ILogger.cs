using System;

namespace NServiceBus.Logging
{
  /// <summary>
  /// 
  /// </summary>
  public interface ILog
  {
    /// <summary>
    /// 
    /// </summary>
    bool IsDebugEnabled { get; }
    /// <summary>
    /// 
    /// </summary>
    bool IsInfoEnabled { get; }
    /// <summary>
    /// 
    /// </summary>
    bool IsWarnEnabled { get; }
    /// <summary>
    /// 
    /// </summary>
    bool IsErrorEnabled { get; }
    /// <summary>
    /// 
    /// </summary>
    bool IsFatalEnabled { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Debug(object message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Debug(object message, Exception exception);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void DebugFormat(string format, params object[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Info(object message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Info(object message, Exception exception);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void InfoFormat(string format, params object[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Warn(object message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Warn(object message, Exception exception);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void WarnFormat(string format, params object[] args);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Error(object message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Error(object message, Exception exception);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void ErrorFormat(string format, params object[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Fatal(object message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    void Fatal(object message, Exception exception);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    void FatalFormat(string format, params object[] args);
  }

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