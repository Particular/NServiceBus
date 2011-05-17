using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using FluentNHibernate.Mapping;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.NHibernate
{
   public class FailureInfo
   {
      public Guid Id { get; set; }
      public bool IsSerializationFailure { get; set; }
      public Address ReplyToAddress { get; set; }
      public TransportMessage Message { get; set; }
      public Exception Exception { get; set; }      
      public string TopmostExceptionMessage { get; set; }
      public string StackTraces { get; set; }

      public FailureInfo(TransportMessage message, Exception exception, bool serializationFailure)
      {                           
         Message = message;
         Exception = exception;
         ReplyToAddress = message.ReplyToAddress;
         TopmostExceptionMessage = exception.Message;
         IsSerializationFailure = serializationFailure;
         StackTraces = CombineStackTraces(exception);
      }

      private static string CombineStackTraces(Exception exception)
      {
         StringBuilder traces = new StringBuilder();
         traces.Append(exception.StackTrace);
         exception = exception.InnerException;
         while (exception != null)
         {
            traces.AppendFormat("-- INNER EXCEPTION --{0}",Environment.NewLine);
            traces.Append(exception.StackTrace);
            exception = exception.InnerException;
         }
         return traces.ToString();
      }

      protected FailureInfo()
      {}
   }

   public class FailureInfoMap : ClassMap<FailureInfo>
   {
      public FailureInfoMap()
      {
         Not.LazyLoad();
         Id(x => x.Id).GeneratedBy.GuidComb();
         Map(x => x.IsSerializationFailure).Not.Nullable();
         Map(x => x.ReplyToAddress).CustomType("Serializable").Length(1000).Not.Nullable();
         Map(x => x.Message).CustomType("Serializable").Length(8001).Not.Nullable();
         Map(x => x.Exception).CustomType("Serializable").Length(8001).Not.Nullable();
         Map(x => x.TopmostExceptionMessage).Length(8001);
         Map(x => x.StackTraces).Length(8001).Not.Nullable();         
      }
   }
}