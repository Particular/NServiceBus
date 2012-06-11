using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
  /// <summary>
  /// 
  /// </summary>
  public class Log : ILog
  {
    private readonly object _logger;

    private static readonly Type LogType = Type.GetType("NLog.Logger, NLog");

    private static readonly Func<object, bool> IsDebugEnabledDelegate;
    private static readonly Func<object, bool> IsInfoEnabledDelegate;
    private static readonly Func<object, bool> IsWarnEnabledDelegate;
    private static readonly Func<object, bool> IsErrorEnabledDelegate;
    private static readonly Func<object, bool> IsFatalEnabledDelegate;
    
    private static readonly Action<object, string> DebugDelegate;
    private static readonly Action<object, string, Exception> DebugExceptionDelegate;
    private static readonly Action<object, string, object[]> DebugFormatDelegate;

    private static readonly Action<object, string> InfoDelegate;
    private static readonly Action<object, string, Exception> InfoExceptionDelegate;
    private static readonly Action<object, string, object[]> InfoFormatDelegate;

    private static readonly Action<object, string> WarnDelegate;
    private static readonly Action<object, string, Exception> WarnExceptionDelegate;
    private static readonly Action<object, string, object[]> WarnFormatDelegate;

    private static readonly Action<object, string> ErrorDelegate;
    private static readonly Action<object, string, Exception> ErrorExceptionDelegate;
    private static readonly Action<object, string, object[]> ErrorFormatDelegate;

    private static readonly Action<object, string> FatalDelegate;
    private static readonly Action<object, string, Exception> FatalExceptionDelegate;
    private static readonly Action<object, string, object[]> FatalFormatDelegate;

    static Log()
    {
      IsDebugEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsDebugEnabled");
      IsInfoEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsInfoEnabled");
      IsWarnEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsWarnEnabled");
      IsErrorEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsErrorEnabled");
      IsFatalEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsFatalEnabled");

      DebugDelegate = LogType.GetInstanceMethodDelegate<string>("Debug");
      DebugExceptionDelegate = LogType.GetInstanceMethodDelegate<string, Exception>("DebugException");
      DebugFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("Debug");

      InfoDelegate = LogType.GetInstanceMethodDelegate<string>("Info");
      InfoExceptionDelegate = LogType.GetInstanceMethodDelegate<string, Exception>("InfoException");
      InfoFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("Info");

      WarnDelegate = LogType.GetInstanceMethodDelegate<string>("Warn");
      WarnExceptionDelegate = LogType.GetInstanceMethodDelegate<string, Exception>("WarnException");
      WarnFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("Warn");

      ErrorDelegate = LogType.GetInstanceMethodDelegate<string>("Error");
      ErrorExceptionDelegate = LogType.GetInstanceMethodDelegate<string, Exception>("ErrorException");
      ErrorFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("Error");

      FatalDelegate = LogType.GetInstanceMethodDelegate<string>("Fatal");
      FatalExceptionDelegate = LogType.GetInstanceMethodDelegate<string, Exception>("FatalException");
      FatalFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("Fatal");
    }

    public Log(object logger)
    {
      _logger = logger;
    }

    public bool IsDebugEnabled
    {
      get { return IsDebugEnabledDelegate(_logger); }
    }

    public bool IsInfoEnabled
    {
      get { return IsInfoEnabledDelegate(_logger); }
    }

    public bool IsWarnEnabled
    {
      get { return IsWarnEnabledDelegate(_logger); }
    }

    public bool IsErrorEnabled
    {
      get { return IsErrorEnabledDelegate(_logger); }
    }

    public bool IsFatalEnabled
    {
      get { return IsFatalEnabledDelegate(_logger); }
    }

    public void Debug(string message)
    {
      DebugDelegate(_logger, message);
    }

    public void Debug(string message, Exception exception)
    {
      DebugExceptionDelegate(_logger, message, exception);
    }

    public void DebugFormat(string format, params object[] args)
    {
      DebugFormatDelegate(_logger, format, args);
    }

    public void Info(string message)
    {
      InfoDelegate(_logger, message);
    }

    public void Info(string message, Exception exception)
    {
      InfoExceptionDelegate(_logger, message,exception);
    }

    public void InfoFormat(string format, params object[] args)
    {
      InfoFormatDelegate(_logger, format, args);
    }

    public void Warn(string message)
    {
      WarnDelegate(_logger, message);
    }

    public void Warn(string message, Exception exception)
    {
      WarnExceptionDelegate(_logger, message, exception);
    }

    public void WarnFormat(string format, params object[] args)
    {
      WarnFormatDelegate(_logger, format, args);
    }

    public void Error(string message)
    {
      ErrorDelegate(_logger, message);
    }

    public void Error(string message, Exception exception)
    {
      ErrorExceptionDelegate(_logger, message, exception);
    }

    public void ErrorFormat(string format, params object[] args)
    {
      ErrorFormatDelegate(_logger, format, args);
    }

    public void Fatal(string message)
    {
      FatalDelegate(_logger, message);
    }

    public void Fatal(string message, Exception exception)
    {
      FatalExceptionDelegate(_logger, message, exception);
    }

    public void FatalFormat(string format, params object[] args)
    {
      FatalFormatDelegate(_logger, format, args);
    }
  }
}