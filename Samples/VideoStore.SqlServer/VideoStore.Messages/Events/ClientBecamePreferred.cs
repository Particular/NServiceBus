using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoStore.Messages.Events
{
    public class ClientBecamePreferred 
    {
        public string ClientId { get; set; }
        public DateTime PreferredStatusExpiresOn { get; set; }
    }
}
