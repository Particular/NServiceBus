namespace VideoStore.Messages.Events
{
    using System;

    public class ClientBecamePreferred 
    {
        public string ClientId { get; set; }
        public DateTime PreferredStatusExpiresOn { get; set; }
    }
}
