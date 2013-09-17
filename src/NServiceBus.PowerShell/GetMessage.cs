namespace NServiceBus.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Messaging;
    using System.Xml;
    using System.Xml.Serialization;

    [Cmdlet(VerbsCommon.Get, "NServiceBusMSMQMessage")]
    public class GetMessage : PSCmdlet
    {
        [Parameter(HelpMessage = "The name of the private queue to search", ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string QueueName { get; set; }

        protected override void ProcessRecord()
        {
            var queueAddress = string.Format("FormatName:DIRECT=OS:{0}\\private$\\{1}", Environment.MachineName,QueueName);

            
            var queue = new MessageQueue(queueAddress);
            var messageReadPropertyFilter = new MessagePropertyFilter();
            messageReadPropertyFilter.SetAll();

            queue.MessageReadPropertyFilter = messageReadPropertyFilter;

            var output = queue.GetAllMessages().Select(m => new
                {
                    m.Id,
                    Headers = ParseHeaders(m),
                    m.ArrivedTime
                });   


            WriteObject(output, true);
        }

        IEnumerable<HeaderInfo> ParseHeaders(Message message)
        {

            IEnumerable<HeaderInfo> result = new List<HeaderInfo>();
            
            if(message.Extension.Length > 0)
            {
                using (var stream = new MemoryStream(message.Extension))
                using (var reader = XmlReader.Create(stream, new XmlReaderSettings { CheckCharacters = false }))

                {
                    result = headerSerializer.Deserialize(reader) as IEnumerable<HeaderInfo>;
                }
            }

            return result;
        }

        private static readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));

        /// <summary>
        /// Represents the structure of header information passed in a TransportMessage.
        /// </summary>
        [Serializable]
        public class HeaderInfo
        {
            /// <summary>
            /// The key used to lookup the value in the header collection.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// The value stored under the key in the header collection.
            /// </summary>
            public string Value { get; set; }
        }
    }
}