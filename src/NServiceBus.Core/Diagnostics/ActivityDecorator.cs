namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    class ActivityDecorator
    {
        public static void PromoteHeadersToTags(Activity activity, Dictionary<string, string> headers)
        {
            if (activity == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (header.Key.StartsWith("NServiceBus.") && !IgnoreHeaders.Contains(header.Key))
                {
                    activity.AddTag(OtNamingConvention(header.Key), header.Value);
                }
            }

            //TODO might be faster to just provide a hardcoded lookup if we're going with an allow-list approach
            string OtNamingConvention(string pascalCasedString)
            {
                // use "nservicebus" instead of "n_service_bus"
                pascalCasedString = pascalCasedString.Replace("NServiceBus", "nservicebus");
                var additionalLength =
                    pascalCasedString.Count(char.IsUpper) - pascalCasedString.Count(c => c == '.');
                var result = new char[pascalCasedString.Length + additionalLength];
                int i = 0;
                foreach (char c in pascalCasedString)
                {
                    if (char.IsUpper(c) && i > 0 && result[i - 1] != '.')
                    {
                        result[i] = '_';
                        i++;
                    }

                    result[i] = char.ToLower(c);
                    i++;
                }

                return new string(result);
            }
        }



        // List of message headers that shouldn't be added as activity tags
        static readonly HashSet<string> IgnoreHeaders = new HashSet<string> { Headers.TimeSent };
    }
}