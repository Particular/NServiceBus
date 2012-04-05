using System;

namespace NServiceBus.Logging.Log4Net
{
  public class Log : ILog
  {
    private readonly log4net.ILog _logger;

    public Log(log4net.ILog logger)
    {
      _logger = logger;
    }

    public bool IsDebugEnabled
    {
      get { return _logger.IsDebugEnabled; }
    }

    public bool IsInfoEnabled
    {
      get { return _logger.IsInfoEnabled; }
    }

    public bool IsWarnEnabled
    {
      get { return _logger.IsWarnEnabled; }
    }

    public bool IsErrorEnabled
    {
      get { return _logger.IsErrorEnabled; }
    }

    public bool IsFatalEnabled
    {
      get { return _logger.IsFatalEnabled; }
    }

    public void Debug(object message)
    {
      _logger.Debug(message);
    }

    public void Debug(object message, Exception exception)
    {
      _logger.Debug(message, exception);
    }

    public void DebugFormat(string format, params object[] args)
    {
      _logger.DebugFormat(format, args);
    }

    public void Info(object message)
    {
      _logger.Info(message);
    }

    public void Info(object message, Exception exception)
    {
      _logger.Info(message, exception);
    }

    public void InfoFormat(string format, params object[] args)
    {
      _logger.InfoFormat(format, args);
    }

    public void Warn(object message)
    {
      _logger.Warn(message);
    }

    public void Warn(object message, Exception exception)
    {
      _logger.Warn(message, exception);
    }

    public void WarnFormat(string format, params object[] args)
    {
      _logger.WarnFormat(format, args);
    }

    public void Error(object message)
    {
      _logger.Error(message);
    }

    public void Error(object message, Exception exception)
    {
      _logger.Error(message, exception);
    }

    public void ErrorFormat(string format, params object[] args)
    {
      _logger.ErrorFormat(format, args);
    }

    public void Fatal(object message)
    {
      _logger.Fatal(message);
    }

    public void Fatal(object message, Exception exception)
    {
      _logger.Fatal(message, exception);
    }

    public void FatalFormat(string format, params object[] args)
    {
      _logger.FatalFormat(format, args);
    }
  }
}