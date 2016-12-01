using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EndpointB;

namespace EndpointB_1
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration.Start("1").GetAwaiter().GetResult();
        }
    }
}
