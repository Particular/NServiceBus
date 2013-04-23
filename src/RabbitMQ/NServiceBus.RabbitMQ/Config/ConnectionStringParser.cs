using IHostConfiguration = EasyNetQ.IHostConfiguration;

namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Text.RegularExpressions;
    using EasyNetQ;
    using System.Linq;

    public class ConnectionStringParser : DbConnectionStringBuilder, IConnectionStringParser
    {
        ConnectionConfiguration connectionConfiguration;
        public IConnectionConfiguration Parse(string connectionString)
        {
            ConnectionString = connectionString;

            try
            {
                connectionConfiguration = new ConnectionConfiguration();

                foreach(var pair in 
                    (from property in typeof(ConnectionConfiguration).GetProperties()
                     let match = Regex.Match(connectionString, string.Format("[^\\w]*{0}=(?<{0}>[^;]+)", property.Name), RegexOptions.IgnoreCase)
                     where match.Success
                     select new { Property = property, match.Groups[property.Name].Value }))
                    pair.Property.SetValue(connectionConfiguration, TypeDescriptor.GetConverter(pair.Property.PropertyType).ConvertFromString(pair.Value),null);

                if (ContainsKey("host"))
                    connectionConfiguration.ParseHosts(this["host"] as string);

                connectionConfiguration.Validate();
                return connectionConfiguration;
            }
            catch (Exception parseException)
            {
                throw new Exception(string.Format("Connection String parsing exception {0}", parseException.Message));
            }
        }
    }
}