namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Transport;

    class FaultMetadataExtractor
    {
        public FaultMetadataExtractor(Dictionary<string, string> staticFaultMetadata, Action<Dictionary<string, string>> headerCustomizations)
        {
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public IDictionary<string, string> Extract(ErrorContext errorContext)
        {
            var metadata = new Dictionary<string, string>(staticFaultMetadata)
            {
                [FaultsHeaderKeys.FailedQ] = errorContext.ReceiveAddress
            };

            SetExceptionMetadata(metadata, errorContext.Exception);

            headerCustomizations(metadata);

            return metadata;
        }

        static void SetExceptionMetadata(Dictionary<string, string> headers, Exception e)
        {
            headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            headers["NServiceBus.ExceptionInfo.Message"] = Truncate(e.GetMessage(), 16384);
            headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            headers["NServiceBus.ExceptionInfo.StackTrace"] = e.ToString();
            headers["NServiceBus.TimeOfFailure"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

            if (e.Data == null)
            {
                return;
            }
            foreach (DictionaryEntry entry in e.Data)
            {
                if (entry.Value == null)
                {
                    continue;
                }
                headers["NServiceBus.ExceptionInfo.Data." + entry.Key] = entry.Value.ToString();
            }
        }

        static string Truncate(string value, int maxLength) =>
            string.IsNullOrEmpty(value)
                ? value
                : (value.Length <= maxLength
                    ? value
                    : value.Substring(0, maxLength));


        readonly Dictionary<string, string> staticFaultMetadata;
        readonly Action<Dictionary<string, string>> headerCustomizations;
    }
}