namespace NServiceBus.Logging
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// Provides logging methods and utility functions.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Gets a value indicating whether logging is enabled for the <see cref="LogLevel.Debug" /> level.
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether logging is enabled for the <see cref="LogLevel.Info" /> level.
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether logging is enabled for the <see cref="LogLevel.Warn" /> level.
        /// </summary>
        bool IsWarnEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether logging is enabled for the <see cref="LogLevel.Error" /> level.
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether logging is enabled for the <see cref="LogLevel.Fatal" /> level.
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Debug" /> level.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Debug(string message);

        /// <summary>
        /// Writes the message and exception at the <see cref="LogLevel.Debug" /> level.
        /// </summary>
        /// <param name="message">A string to be written.</param>
        /// <param name="exception">An exception to be logged.</param>
        void Debug(string message, Exception exception);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Debug" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        [StringFormatMethod("format")]
        void DebugFormat(string format, object argument1);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Debug" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        [StringFormatMethod("format")]
        void DebugFormat(string format, object argument1, object argument2);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Debug" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        /// <param name="argument3">The third argument to format.</param>
        [StringFormatMethod("format")]
        void DebugFormat(string format, object argument1, object argument2, object argument3);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Debug" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="args">Arguments to format.</param>
        [StringFormatMethod("format")]
        void DebugFormat(string format, params object[] args);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Info" /> level.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Info(string message);

        /// <summary>
        /// Writes the message and exception at the <see cref="LogLevel.Info" /> level.
        /// </summary>
        /// <param name="message">A string to be written.</param>
        /// <param name="exception">An exception to be logged.</param>
        void Info(string message, Exception exception);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Info" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        [StringFormatMethod("format")]
        void InfoFormat(string format, object argument1);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Info" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        [StringFormatMethod("format")]
        void InfoFormat(string format, object argument1, object argument2);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Info" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        /// <param name="argument3">The third argument to format.</param>
        [StringFormatMethod("format")]
        void InfoFormat(string format, object argument1, object argument2, object argument3);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Info" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="args">Arguments to format.</param>
        [StringFormatMethod("format")]
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Warn" /> level.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Warn(string message);

        /// <summary>
        /// Writes the message and exception at the <see cref="LogLevel.Warn" /> level.
        /// </summary>
        /// <param name="message">A string to be written.</param>
        /// <param name="exception">An exception to be logged.</param>
        void Warn(string message, Exception exception);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Warn" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        [StringFormatMethod("format")]
        void WarnFormat(string format, object argument1);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Warn" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        [StringFormatMethod("format")]
        void WarnFormat(string format, object argument1, object argument2);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Warn" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        /// <param name="argument3">The third argument to format.</param>
        [StringFormatMethod("format")]
        void WarnFormat(string format, object argument1, object argument2, object argument3);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Warn" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="args">Arguments to format.</param>
        [StringFormatMethod("format")]
        void WarnFormat(string format, params object[] args);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Error" /> level.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Error(string message);

        /// <summary>
        /// Writes the message and exception at the <see cref="LogLevel.Error" /> level.
        /// </summary>
        /// <param name="message">A string to be written.</param>
        /// <param name="exception">An exception to be logged.</param>
        void Error(string message, Exception exception);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Error" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        [StringFormatMethod("format")]
        void ErrorFormat(string format, object argument1);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Error" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        [StringFormatMethod("format")]
        void ErrorFormat(string format, object argument1, object argument2);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Error" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        /// <param name="argument3">The third argument to format.</param>
        [StringFormatMethod("format")]
        void ErrorFormat(string format, object argument1, object argument2, object argument3);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Error" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="args">Arguments to format.</param>
        [StringFormatMethod("format")]
        void ErrorFormat(string format, params object[] args);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Fatal" /> level.
        /// </summary>
        /// <param name="message">Log message.</param>
        void Fatal(string message);

        /// <summary>
        /// Writes the message and exception at the <see cref="LogLevel.Fatal" /> level.
        /// </summary>
        /// <param name="message">A string to be written.</param>
        /// <param name="exception">An exception to be logged.</param>
        void Fatal(string message, Exception exception);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Fatal" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        [StringFormatMethod("format")]
        void FatalFormat(string format, object argument1);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Fatal" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        [StringFormatMethod("format")]
        void FatalFormat(string format, object argument1, object argument2);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Fatal" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="argument1">The first argument to format.</param>
        /// <param name="argument2">The second argument to format.</param>
        /// <param name="argument3">The third argument to format.</param>
        [StringFormatMethod("format")]
        void FatalFormat(string format, object argument1, object argument2, object argument3);

        /// <summary>
        /// Writes the message at the <see cref="LogLevel.Fatal" /> level using the specified <paramref name="format" /> provider
        /// and arguments.
        /// </summary>
        /// <param name="format">A string containing format items.</param>
        /// <param name="args">Arguments to format.</param>
        [StringFormatMethod("format")]
        void FatalFormat(string format, params object[] args);
    }
}