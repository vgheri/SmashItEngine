using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace vgheri.SmashItEngine.core
{
    internal class HttpActionResult
    {
        internal bool RequestTimedOut { get; set; }
        internal HttpResponseMessage ResponseMessage { get; set; }
        internal double ResponseTime { get; set; }
        internal int ConcurrentUsers { get; set; }
    }
}
