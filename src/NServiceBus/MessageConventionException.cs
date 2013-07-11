using System;

namespace NServiceBus
{
  /// <summary>
  /// 
  /// </summary>
  public class MessageConventionException : Exception
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public MessageConventionException(string message, Exception innerException) : base(message, innerException)
    {
    }
  }
}