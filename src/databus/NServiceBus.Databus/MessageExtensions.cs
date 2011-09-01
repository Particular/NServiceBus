using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NServiceBus.DataBus
{
	using System;

	public static class MessageExtensions
    {
        public static IEnumerable<PropertyInfo> DataBusProperties(this object message)
        {
            return message.GetType().GetProperties()
                .Where(t => typeof (IDataBusProperty).IsAssignableFrom(t.PropertyType));

        }
        public static IEnumerable<IDataBusProperty> DataBusPropertiesWithValues(this object message)
        {
            return message.DataBusProperties()
				.Select(p => p.GetValue(message,null) as IDataBusProperty)
				.Where(p=>p != null && p.HasValue);    
        }

        public static TimeSpan TimeToBeReceived(this object message)
		 {
		 	var attributes = message.GetType().GetCustomAttributes(typeof (TimeToBeReceivedAttribute), true)
		 		.Select(s => s as TimeToBeReceivedAttribute);

			if (attributes.Any())
				return attributes.Last().TimeToBeReceived;

		 	return TimeSpan.MaxValue;
		 }
    }
}