using System;

namespace NServiceBus.Logging.Loggers
{
  /// <summary>
  /// 
  /// </summary>
  public class ConsoleLogger : ILog
  {
    /// <summary>
    /// 
    /// </summary>
    public bool IsDebugEnabled
    {
      get { return true; }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsInfoEnabled
    {
      get { return true; }
    }

    public bool IsWarnEnabled
    {
      get { return true; }
    }

    public bool IsErrorEnabled
    {
      get { return true; }
    }

    public bool IsFatalEnabled
    {
      get { return true; }
    }

    public void Debug(string message)
    {
      Console.WriteLine(message);
    }

    public void Debug(string message, Exception exception)
    {
      Console.WriteLine(message);
      Console.WriteLine(exception);
    }

    public void DebugFormat(string format, params object[] args)
    {
      Console.WriteLine(format, args);
    }

    public void Info(string message)
    {
      Console.WriteLine(message);
    }

    public void Info(string message, Exception exception)
    {
      Console.WriteLine(message);
      Console.WriteLine(exception);
    }

    public void InfoFormat(string format, params object[] args)
    {
      Console.WriteLine(format, args);
    }

    public void Warn(string message)
    {
      Console.WriteLine(message);
    }

    public void Warn(string message, Exception exception)
    {
      Console.WriteLine(message);
      Console.WriteLine(exception);
    }

    public void WarnFormat(string format, params object[] args)
    {
      Console.WriteLine(format, args);
    }

    public void Error(string message)
    {
      Console.WriteLine(message);
    }

    public void Error(string message, Exception exception)
    {
      Console.WriteLine(message);
      Console.WriteLine(exception);
    }

    public void ErrorFormat(string format, params object[] args)
    {
      Console.WriteLine(format, args);
    }

    public void Fatal(string message)
    {
      Console.WriteLine(message);
    }

    public void Fatal(string message, Exception exception)
    {
      Console.WriteLine(message);
      Console.WriteLine(exception);
    }

    public void FatalFormat(string format, params object[] args)
    {
      Console.WriteLine(format, args);
    }
  }
}