using System;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace retriable_consumer
{
    public class ExternalService
    {
        private readonly string externalServiceUrl;
        private readonly ILogger logger;
        private readonly RestClient client;

        public ExternalService(string externalServiceUrl)
        {
            this.externalServiceUrl = externalServiceUrl;
            logger = Program.LoggerFactory.CreateLogger<ExternalService>();
            client = new RestClient(externalServiceUrl);
        }

        public void Call()
        {
            var request = new RestRequest(Method.Get);
            request.Timeout = 500;
            logger.LogDebug($"Execute async call of web service {externalServiceUrl}");
            var response = client.ExecuteAsync(request).GetAwaiter().GetResult();
            int code = (int)response.StatusCode;
            if (code >= 300)
            {
                logger.LogWarning($"Error from web service with http code {code} and content {response.Content}");
                throw new Exception(response.Content);
            }
            logger.LogDebug($"Call of web service {externalServiceUrl} is finished, all works !");
        }
    }
}