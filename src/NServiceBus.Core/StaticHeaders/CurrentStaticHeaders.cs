namespace NServiceBus
{
    using System.Collections.Generic;

    sealed class CurrentStaticHeaders : Dictionary<string, string>
    {
        public CurrentStaticHeaders(int capacity) : base(capacity)
        {
        }
    }
}