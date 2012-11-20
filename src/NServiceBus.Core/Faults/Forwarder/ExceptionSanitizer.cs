namespace NServiceBus.Faults.Forwarder
{
    using System;

    /// <summary>
    /// Exception Sanitizer
    /// </summary>
   public static class ExceptionSanitizer
   {
       /// <summary>
       /// Creates a new exception out of Original Exception
       /// </summary>
       /// <param name="original">Original exception</param>
       /// <returns></returns>
      public static Exception Sanitize(Exception original)
      {
         throw new NotImplementedException();         
      }
   }
}