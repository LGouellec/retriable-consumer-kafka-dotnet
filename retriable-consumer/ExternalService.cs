using System;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace retriable_consumer
{
    public class ExternalService
    {
        private readonly string externalServiceUrl;
        private readonly int durationSleepIntervalMs;
        private readonly bool simulateExternalService;
        private readonly int percentageFailure;
        private readonly ILogger logger;
        private readonly RestClient client;
        private readonly Random random = new();
        
        public ExternalService(string externalServiceUrl, int durationSleepIntervalMs, bool simulateExternalService, int percentageFailure)
        {
            this.externalServiceUrl = externalServiceUrl;
            this.durationSleepIntervalMs = durationSleepIntervalMs;
            this.simulateExternalService = simulateExternalService;
            this.percentageFailure = percentageFailure;
            logger = Program.LoggerFactory.CreateLogger<ExternalService>();
            client = new RestClient(externalServiceUrl);        
        }

        public void Call(ConsumeResult<string, string> record)
        {
            if(simulateExternalService)
                SimulateCall(record);
            else
                RealCall(record);
        }

        private void SimulateCall(ConsumeResult<string, string> record)
        {
            logger.LogDebug($"Simulate a call to external service for message (offset:{record.Offset}, topic/partition:{record.TopicPartition}, key: {record.Message.Key}, value: {record.Message.Value})...");
            var result = random.Next(100);
            Thread.Sleep(durationSleepIntervalMs);
            if(result < percentageFailure)
                throw new Exception("Simulate call to external service failed");
        }

        private void RealCall(ConsumeResult<string, string> record)
        {
            var request = new RestRequest(Method.Get);
            request.Timeout = 500;
            logger.LogDebug($"Execute async call of web service {externalServiceUrl} for message (offset:{record.Offset}, topic/partition:{record.TopicPartition}, key: {record.Message.Key}, value: {record.Message.Value})...");
            var response = client.ExecuteAsync(request).GetAwaiter().GetResult();
            int code = (int)response.StatusCode;
            if (code >= 300)
            {
                Thread.Sleep(durationSleepIntervalMs);
                logger.LogWarning($"Error from web service with http code {code} and content {response.Content}");
                throw new Exception(response.Content);
            }
            logger.LogDebug($"Call of web service {externalServiceUrl} is finished, all works !");
        }
    }
}