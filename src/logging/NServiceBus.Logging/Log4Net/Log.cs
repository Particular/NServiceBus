using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Log4Net
{
  /// <summary>
  /// 
  /// </summary>
  public class Log : ILog
  {
    private readonly object _logger;

    private static readonly Type LogType = Type.GetType("log4net.ILog, log4net");

    private static readonly Func<object, bool> IsDebugEnabledDelegate;
    private static readonly Func<object, bool> IsInfoEnabledDelegate;
    private static readonly Func<object, bool> IsWarnEnabledDelegate;
    private static readonly Func<object, bool> IsErrorEnabledDelegate;
    private static readonly Func<object, bool> IsFatalEnabledDelegate;
    
    private static readonly Action<object, object> DebugDelegate;
    private static readonly Action<object, object, Exception> DebugExceptionDelegate;
    private static readonly Action<object, string, object[]> DebugFormatDelegate;
    
    private static readonly Action<object, object> InfoDelegate;
    private static readonly Action<object, object, Exception> InfoExceptionDelegate;
    private static readonly Action<object, string, object[]> InfoFormatDelegate;
    
    private static readonly Action<object, object> WarnDelegate;
    private static readonly Action<object, object, Exception> WarnExceptionDelegate;
    private static readonly Action<object, string, object[]> WarnFormatDelegate;
    
    private static readonly Action<object, object> ErrorDelegate;
    private static readonly Action<object, object, Exception> ErrorExceptionDelegate;
    private static readonly Action<object, string, object[]> ErrorFormatDelegate;
    
    private static readonly Action<object, object> FatalDelegate;
    private static readonly Action<object, object, Exception> FatalExceptionDelegate;
    private static readonly Action<object, string, object[]> FatalFormatDelegate;

    static Log()
    {
      IsDebugEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsDebugEnabled");
      IsInfoEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsInfoEnabled");
      IsWarnEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsWarnEnabled");
      IsErrorEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsErrorEnabled");
      IsFatalEnabledDelegate = LogType.GetInstancePropertyDelegate<bool>("IsFatalEnabled");

      DebugDelegate = LogType.GetInstanceMethodDelegate<object>("Debug");
      DebugExceptionDelegate = LogType.GetInstanceMethodDelegate<object, Exception>("Debug");
      DebugFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("DebugFormat");

      InfoDelegate = LogType.GetInstanceMethodDelegate<object>("Info");
      InfoExceptionDelegate = LogType.GetInstanceMethodDelegate<object, Exception>("Info");
      InfoFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("InfoFormat");

      WarnDelegate = LogType.GetInstanceMethodDelegate<object>("Warn");
      WarnExceptionDelegate = LogType.GetInstanceMethodDelegate<object, Exception>("Warn");
      WarnFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("WarnFormat");

      ErrorDelegate = LogType.GetInstanceMethodDelegate<object>("Error");
      ErrorExceptionDelegate = LogType.GetInstanceMethodDelegate<object, Exception>("Error");
      ErrorFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("ErrorFormat");

      FatalDelegate = LogType.GetInstanceMethodDelegate<object>("Fatal");
      FatalExceptionDelegate = LogType.GetInstanceMethodDelegate<object, Exception>("Fatal");
      FatalFormatDelegate = LogType.GetInstanceMethodDelegate<string, object[]>("FatalFormat");
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

    public void Debug(object message)
    {
      DebugDelegate(_logger, message);
    }

    public void Debug(object message, Exception exception)
    {
      DebugExceptionDelegate(_logger, message, exception);
    }

    public void DebugFormat(string format, params object[] args)
    {
      DebugFormatDelegate(_logger, format, args);
    }

    public void Info(object message)
    {
      InfoDelegate(_logger, message);
    }

    public void Info(object message, Exception exception)
    {
      InfoExceptionDelegate(_logger, message,exception);
    }

    public void InfoFormat(string format, params object[] args)
    {
      InfoFormatDelegate(_logger, format, args);
    }

    public void Warn(object message)
    {
      WarnDelegate(_logger, message);
    }

    public void Warn(object message, Exception exception)
    {
      WarnExceptionDelegate(_logger, message, exception);
    }

    public void WarnFormat(string format, params object[] args)
    {
      WarnFormatDelegate(_logger, format, args);
    }

    public void Error(object message)
    {
      ErrorDelegate(_logger, message);
    }

    public void Error(object message, Exception exception)
    {
      ErrorExceptionDelegate(_logger, message, exception);
    }

    public void ErrorFormat(string format, params object[] args)
    {
      ErrorFormatDelegate(_logger, format, args);
    }

    public void Fatal(object message)
    {
      FatalDelegate(_logger, message);
    }

    public void Fatal(object message, Exception exception)
    {
      FatalExceptionDelegate(_logger, message, exception);
    }

    public void FatalFormat(string format, params object[] args)
    {
      FatalFormatDelegate(_logger, format, args);
    }
  }
}