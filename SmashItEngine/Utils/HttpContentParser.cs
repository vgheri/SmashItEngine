using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vgheri.SmashItEngine.core.Utils
{
    internal sealed class HttpContentParser
    {
        internal static IEnumerable<KeyValuePair<string, string>> ParseFormUrlEncodedContent(string content)
        {
            var collection = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("Error: http content cannot be null or empty.");
            }
            var coupleSplit = content.Split('&');
            string[] keyAndValue = null;
            foreach (var couple in coupleSplit)
            {
                keyAndValue = couple.Split('=');
                if (keyAndValue.Length != 2)
                {
                    throw new ArgumentException("Error: malformed content for content type FormUrlEncoded");
                }
                var key = keyAndValue[0];
                var value = keyAndValue[1];
                var pair = new KeyValuePair<string, string>(key, value);
                collection.Add(pair);
            }
            return collection.AsEnumerable();
        }
    }
}
