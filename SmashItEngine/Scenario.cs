using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using vgheri.SmashItEngine.core.Utils;

namespace vgheri.SmashItEngine.core
{
    public sealed class Scenario
    {
        /// <summary>
        /// Total number of users that will populate this scenario
        /// </summary>
        internal int Users { get; private set; }

        /// <summary>
        /// The url of the remote host to test
        /// </summary>
        internal Uri BaseAddress { get; private set; }

        /// <summary>
        /// The duration of the test, expressed in seconds
        /// </summary>
        internal int Duration { get; private set; }
        
        /// <summary>
        /// The length of the pause between steps, expressed in seconds
        /// </summary>
        internal int PauseDuration { get; private set; }

        /// <summary>
        /// The amount of milliseconds to wait before considering the request as timed out
        /// </summary>
        internal double Timeout { get; private set; }

        /// <summary>
        /// Describes the model that dictates how the number of concurrent users grows during the lifespan of the test
        /// </summary>
        internal UserGrowthProgressionModel UserGrowthProgressionModel { get; private set; }

        internal List<HttpRequestMessage> Steps { get; private set; }

        /// <summary>
        /// Creates a new test scenario
        /// </summary>
        /// <param name="totalUsers">The total number of simulated users that will be created during the test.</param>
        /// <param name="targetAddress">The URL of the remote host to test. Must be a valid absolute URI.</param>
        /// <param name="testDuration">The duration of the test, expressed in seconds. Must be between 30 and 300 seconds.</param>
        /// <param name="pauseDuration">
        /// The duration of the pause between each step, expressed in seconds. Must be between 0 and 10 seconds.
        /// Defaults to 3 seconds.
        /// </param>
        /// <param name="userGrowthProgressionModel">
        /// Describes the model that dictates how the number of concurrent users grows during the lifespan of the test.
        /// Defaults to linear.
        /// </param>
        internal Scenario(int totalUsers, string targetAddress, int testDuration,
            UserGrowthProgressionModel userGrowthProgressionModel, double timeout, int pauseDuration = 3) 
        {
            bool throwsException = false;
            StringBuilder errorMessageBuilder = new StringBuilder();
            if (totalUsers < 1)
            {
                throwsException = true;
                errorMessageBuilder.AppendLine("Total number of users must be greater than 0.");
            }
            else 
            {
                this.Users = totalUsers;
            }
            if (!Uri.IsWellFormedUriString(targetAddress, UriKind.Absolute))
            {
                throwsException |= true;
                errorMessageBuilder.AppendLine("The supplied target address is not valid.");
            }
            else
            {
                this.BaseAddress = new Uri(targetAddress);
            }
            if (testDuration < 30 || testDuration > 300)
            {
                throwsException |= true;
                errorMessageBuilder.AppendLine("Allowed test length is between 30 and 300 seconds.");
            }
            else
            {
                this.Duration = testDuration;
            }
            if (timeout < 0)
            {
                throwsException |= true;
                errorMessageBuilder.AppendLine("Time out must be greater than 0 milliseconds.");
            }
            else
            {
                this.Timeout = timeout;
            }
            if (pauseDuration < 0 || pauseDuration > 10)
            {
                throwsException |= true;
                errorMessageBuilder.AppendLine("Allowed pause length is between 0 and 10 seconds.");
            }
            else
            {
                this.PauseDuration = pauseDuration;
            }

            if (throwsException)
            {
                throw new ArgumentException("Error(s): " + errorMessageBuilder.ToString());
            }

            this.UserGrowthProgressionModel = userGrowthProgressionModel;

            this.Steps = new List<HttpRequestMessage>();
        }

        /// <summary>
        /// Adds a step to the load test plan. Each step is an HTTP request.
        /// </summary>
        /// <param name="headers">Collection of headers for this request</param>
        /// <param name="verb">The method to use</param>
        /// <param name="endpoint">The relative uri for the endpoint</param>
        /// <param name="contentType">The content-type of the request, if any</param>
        /// <param name="bodyContent">The content of the request, if any</param>
        /// <param name="mimeType">The mime type of the request, if any. Defaults to "application/json".</param>
        public Scenario AddStep(Dictionary<string, string> headers, string verb, string endpoint, 
            HttpRequestContentType? contentType, string bodyContent, string mimeType = "application/json")
        {
            HttpMethod method = ParseMethod(verb);
            Uri uri = CreateUri(endpoint);
            HttpContent content = CreateContentType(contentType, bodyContent, mimeType);            
            
            HttpRequestMessage step = new HttpRequestMessage(method, endpoint);
            
            if (headers != null && headers.Keys.Count > 0)
            {                
                foreach (var key in headers.Keys)
                {
                    step.Headers.Add(key, headers[key]);
                }
            }

            if (content != null)
            {
                step.Content = content;
            }

            this.Steps.Add(step);
            return this;
        }

        public HttpRequestMessage CreateStep(HttpRequestMessage step)
        {
            HttpRequestMessage copy = new HttpRequestMessage();
            copy.Content = step.Content;
            if (step.Headers != null)
            {
                foreach (var header in step.Headers)
                {
                    copy.Headers.Add(header.Key, header.Value);
                }
            }            
            copy.Method = step.Method;
            if (step.Properties != null)
            {
                foreach (var property in step.Properties)
                {
                    copy.Properties.Add(property.Key, property.Value);
                }
            }            
            copy.RequestUri = step.RequestUri;
            copy.Version = step.Version;
            return copy;
        }

        #region Private Methods

        private HttpMethod ParseMethod(string verb)
        {
            HttpMethod method = null;
            verb = verb.ToUpper();

            switch (verb)
            {
                case "GET":
                    method = HttpMethod.Get;
                    break;
                case "POST":
                    method = HttpMethod.Post;
                    break;
                case "PUT":
                    method = HttpMethod.Put;
                    break;
                case "DELETE":
                    method = HttpMethod.Delete;
                    break;
                default:
                    method = new HttpMethod(verb);
                    break;
            }
            return method;
        }

        private Uri CreateUri(string endpoint)
        {
            if (!Uri.IsWellFormedUriString(endpoint, UriKind.Relative))
            {
                throw new ArgumentException("Error: the specified endpoint is not a valid relative URI.");
            }
            return new Uri(this.BaseAddress, endpoint);            
        }

        private HttpContent CreateContentType(HttpRequestContentType? contentType, string bodyContent, string mimeType)
        {
            HttpContent content = null;
            if (contentType.HasValue && !string.IsNullOrEmpty(bodyContent))
            {
                if (contentType == HttpRequestContentType.FormUrlEncodedContent)
                {
                    var collection = HttpContentParser.ParseFormUrlEncodedContent(bodyContent);
                    content = new FormUrlEncodedContent(collection);
                }
                else if (contentType == HttpRequestContentType.StringContent)
                {
                    content = new StringContent(bodyContent, Encoding.UTF8, mimeType);
                }
                else
                {
                    throw new NotImplementedException("Content type not yet implemented. Try FormUrlEncoded or StringContent");
                }
            }
            return content;
        }
        #endregion
    }
}
