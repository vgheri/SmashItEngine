using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vgheri.SmashItEngine.core
{
    public interface IEngine
    {
        IEngine AddStep(Dictionary<string, string> headers, string verb, string endpoint,
            HttpRequestContentType? contentType, string bodyContent, string mimeType = "application/json");
        void Run();
    }
}
