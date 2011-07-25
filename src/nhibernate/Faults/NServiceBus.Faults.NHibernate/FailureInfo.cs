using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.NHibernate
{
   public class FailureInfo
   {
      public Guid Id { get; set; }
      public bool IsSerializationFailure { get; set; }
      public string ReturnAddress { get; set; }
      public TransportMessage Message { get; set; }
      public Exception Exception { get; set; }      
      public string TopmostExceptionMessage { get; set; }
      public string StackTraces { get; set; }

      public FailureInfo(TransportMessage message, Exception exception, bool serializationFailure)
      {                           
         Message = message;
         Exception = exception;
         ReturnAddress = message.ReplyToAddress == null ? string.Empty : message.ReplyToAddress.ToString();
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

  public class FailureInfoMapping : ClassMapping<FailureInfo>
  {
    public FailureInfoMapping()
    {
      Lazy(false);
      Id(x => x.Id, x => x.Generator(Generators.GuidComb));
      Property(x => x.IsSerializationFailure, x => x.NotNullable(true));
      Property(x => x.ReturnAddress, x => { x.Length(1000); x.NotNullable(true); });
      Property(x => x.Message, x => { x.Type(global::NHibernate.NHibernateUtil.Serializable); x.Length(8001); x.NotNullable(true); });
      Property(x => x.Exception, x => { x.Type(global::NHibernate.NHibernateUtil.Serializable); x.Length(8001); x.NotNullable(true); });
      Property(x => x.TopmostExceptionMessage, x => x.Length(8001));
      Property(x => x.StackTraces, x => { x.Length(8001); x.NotNullable(true); });
    }
  }

   //public class FailureInfoMap : ClassMap<FailureInfo>
   //{
   //   public FailureInfoMap()
   //   {
   //      Not.LazyLoad();
   //      Id(x => x.Id).GeneratedBy.GuidComb();
   //      Map(x => x.IsSerializationFailure).Not.Nullable();
   //      Map(x => x.ReturnAddress).Length(1000).Not.Nullable();
   //      Map(x => x.Message).CustomType("Serializable").Length(8001).Not.Nullable();
   //      Map(x => x.Exception).CustomType("Serializable").Length(8001).Not.Nullable();
   //      Map(x => x.TopmostExceptionMessage).Length(8001);
   //      Map(x => x.StackTraces).Length(8001).Not.Nullable();         
   //   }
   //}
}